using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using System.Linq;
using Windows.UI.Text;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace SnooStream.Converters
{
	public class LinkMetadataConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var linkViewModel = value as LinkViewModel;
			var rtb = new RichTextBlock();
			var pp = new Paragraph();
			rtb.IsTextSelectionEnabled = false;
			rtb.Blocks.Add(pp);
			rtb.FontSize = 14;
			rtb.FontStyle = Windows.UI.Text.FontStyle.Normal;
			rtb.FontWeight = FontWeights.Normal;
			List<Inline> inlinesCollection = new List<Inline>();
			
			var subredditLink = new Run { Text = linkViewModel.Link.Subreddit };
			var authorLink = new Run { Text = linkViewModel.Author };

			if (linkViewModel.Link.Over18)
				inlinesCollection.Add(new Run { Text = "NSFW", Foreground = new SolidColorBrush(Colors.Red)});

			if (!string.IsNullOrWhiteSpace(linkViewModel.Link.LinkFlairText))
				inlinesCollection.Add(new Run { Text = linkViewModel.Link.LinkFlairText });

			if (linkViewModel.FromMultiReddit)
				inlinesCollection.Add(subredditLink);

			inlinesCollection.Add(authorLink);
			inlinesCollection.Add(new Run { Text = TimeRelationConverter.GetRelationString(linkViewModel.CreatedUTC) });
			inlinesCollection.Add(new Run { Text = DomainConverter.GetDomain(linkViewModel.Url) });

			for (int i = 0; i < inlinesCollection.Count; i++)
			{
				pp.Inlines.Add(inlinesCollection[i]);
				if (i == (inlinesCollection.Count - 1))
				{
				}
				else
				{
					pp.Inlines.Add(new Run { Text = "  \u2022  " });
				}
			}

			rtb.Tapped += (sender, args) =>
			{
				var textPointer = rtb.GetPositionFromPoint(args.GetPosition(rtb));
				var element = textPointer.Parent as TextElement;
				while (element != null && !(element is Run))
				{
					if (element.ContentStart != null
						&& element != element.ElementStart.Parent)
					{
						element = element.ElementStart.Parent as TextElement;
					}
					else
					{
						element = null;
					}
				}

				if (element == null) return;

				var run = element as Run;
				if (run == subredditLink)
					linkViewModel.GotoSubreddit.Execute(null);
				else if (run == authorLink)
					linkViewModel.GotoUserDetails.Execute(null);
				
			};

			return rtb;

		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
