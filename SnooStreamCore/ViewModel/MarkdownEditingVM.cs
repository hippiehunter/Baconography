﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
	public class MarkdownEditingVM : ViewModelBase
	{
		Action<string> _textChanged;
		private string _initialText;
		public MarkdownEditingVM(string text, Action<string> textChanged)
		{
			_textChanged = textChanged;
			Text = _initialText = text;
		}

		public void Cancel()
		{
			Text = _initialText;
		}

		public string PostingAs
		{
			get
			{
				return SnooStreamViewModel.RedditUserState.Username;
			}
		}

		private object _markdownDom;
		public object Markdown
		{
			get
			{
				return _markdownDom;
			}
			set
			{
				_markdownDom = value;
				RaisePropertyChanged("Markdown");
			}
		}

		private string _text;
		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
				_text = value;
				RaisePropertyChanged("Text");
				_textChanged(value);
				//if the text hasnt changed for 3 seconds its a good idea to refresh the markdown rendering
				SnooStreamViewModel.SystemServices.StartThreadPoolTimer((args) =>
					{
						if (_text == value)
						{
							var madeMarkdown = SnooStreamViewModel.MarkdownProcessor.Process(value);
							if (madeMarkdown != null && madeMarkdown.MarkdownDom != null)
							{
								SnooStreamViewModel.SystemServices.QueueNonCriticalUI(() =>
									{
										Markdown = madeMarkdown.MarkdownDom;
									});
							}
						}
						SnooStreamViewModel.SystemServices.StopTimer(args);
						return Task.FromResult(true);
					}, TimeSpan.FromSeconds(1));
			}
		}

		private int _selectionLength;
		public int SelectionLength
		{
			get
			{
				return _selectionLength;
			}
			set
			{
				_selectionLength = value;
				RaisePropertyChanged("SelectionLength");
			}
		}

		private int _selectionStart;
		public int SelectionStart
		{
			get
			{
				return _selectionStart;
			}
			set
			{
				_selectionStart = value;
				RaisePropertyChanged("SelectionStart");
			}
		}

		private Tuple<int, int, string> SurroundSelection(int startPosition, int endPosition, string startText, string newTextFormat)
		{
			//split selection into multiple lines
			//for each line in the selection we apply its body to the newTextFormat string via string.format
			//if we only had a single line return the selection span as the modified position of just the original text
			//if we had multiple lines the selection span should be the entire replace string block

			if (string.IsNullOrEmpty(startText))
				startPosition = endPosition = 0;

			var selectedText = string.IsNullOrEmpty(startText) ? "" : startText.Substring(startPosition, endPosition - startPosition);

			string splitter = "\n";
			if (selectedText.Contains("\r\n"))
			{
				splitter = "\r\n";
			}

			var preText = (string.IsNullOrEmpty(startText) || startPosition == 0) ? "" : startText.Substring(0, startPosition);
			var postText = (string.IsNullOrEmpty(startText) || endPosition == startText.Length) ? "" : startText.Substring(endPosition + 1);

			var selectedTextLines = selectedText.Split(new string[] { splitter }, StringSplitOptions.None);
			if (selectedTextLines.Length > 1)
			{
				var formattedText = string.Join(splitter, selectedTextLines.Select(str => string.Format(newTextFormat, str)));
				var newText = preText + formattedText + postText;
				return Tuple.Create(startPosition, startPosition + formattedText.Length, newText);
			}
			else
			{
				var newText = preText + string.Format(newTextFormat, selectedText) + postText;
				var formatOffset = newTextFormat.IndexOf("{0}");
				return Tuple.Create(startPosition + formatOffset, endPosition + formatOffset, newText);
			}

		}

		private string _boldFormattingString = "**{0}**";
		private string _italicFormattingString = "*{0}*";
		private string _strikeFormattingString = "~~{0}~~";
		private string _superFormattingString = "^{0}";
		private string _linkFormattingString = "[{0}](the-url-goes-here)";
		private string _quoteFormattingString = ">{0}";
		private string _codeFormattingString = "    {0}";
		private string _bulletFormattingString = "*{0}";
		private string _numberFormattingString = "1. {0}";
        private string _disapprovalFormattingString = "&#x0CA0;_&#x0CA0; {0}";

		public RelayCommand InsertBold { get { return new RelayCommand(AddBoldImpl); } }
		public RelayCommand InsertItalic { get { return new RelayCommand(AddItalicImpl); } }
		public RelayCommand InsertStrike { get { return new RelayCommand(AddStrikeImpl); } }
		public RelayCommand InsertSuper { get { return new RelayCommand(AddSuperImpl); } }
		public RelayCommand InsertLink { get { return new RelayCommand(AddLinkImpl); } }
		public RelayCommand InsertQuote { get { return new RelayCommand(AddQuoteImpl); } }
		public RelayCommand InsertCode { get { return new RelayCommand(AddCodeImpl); } }
		public RelayCommand InsertBullets { get { return new RelayCommand(AddBulletsImpl); } }
		public RelayCommand InsertNumbers { get { return new RelayCommand(AddNumbersImpl); } }
		public RelayCommand InsertDisapproval { get { return new RelayCommand(AddDisapprovalImpl); } }

		private void AddDisapprovalImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _disapprovalFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddBoldImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _boldFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddItalicImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _italicFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddStrikeImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _strikeFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddSuperImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _superFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddLinkImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _linkFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddQuoteImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _quoteFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddCodeImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _codeFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddBulletsImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _bulletFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}

		private void AddNumbersImpl()
		{
			var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, Text, _numberFormattingString);
			Text = surroundedTextTpl.Item3;
			SelectionStart = surroundedTextTpl.Item1;
			SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
		}
	}
}
