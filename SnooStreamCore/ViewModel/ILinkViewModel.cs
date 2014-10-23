using SnooSharp;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
	public interface ILinkViewModel
	{
		AuthorFlairKind AuthorFlair { get; }
		string AuthorFlairText { get; set; }
		bool HasAuthorFlair { get; }
		string Author { get; }
		string Subreddit { get; }
		string Title { get; }
		string Url { get; }
		DateTime CreatedUTC { get; }
		string Id { get; }
		ContentViewModel Content { get; }
	}
}
