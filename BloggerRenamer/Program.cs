using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;
using DotNetOpenAuth.OAuth2;
using Google.Apis.Services;
using System.Diagnostics;
using Google.Apis.Util;
using System.Text.RegularExpressions;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Authentication.OAuth2;

namespace BloggerRen
{
	class Program
	{
		public static void Main(string[] args)
		{
			//Please change the apiKey to your API key
			// and also Client_ID and Client_Secret
			string apiKey = "API_KEY";
			string blogUrl = @"BLOG_LINK";

			//OAuth

			NativeApplicationClient provider = new NativeApplicationClient(GoogleAuthenticationServer.Description)
			{
				ClientIdentifier = "CLIENT_ID",
				ClientSecret = "CLIENT_SECRET"
			};

			OAuth2Authenticator<NativeApplicationClient> auth = new OAuth2Authenticator<NativeApplicationClient>(provider, getAuth);

			//New blogger service
			BloggerService blogService = new BloggerService(new BaseClientService.Initializer()
			{
				Authenticator = auth,
				ApplicationName = "BloggerRenamer"
			});

			//Get a blogs resource by URL
			BlogsResource.GetByUrlRequest blogInfo = blogService.Blogs.GetByUrl(blogUrl);
			blogInfo.Key = apiKey;
			Blog blog = blogInfo.Execute();
			Console.WriteLine(blog.Id);

			//Get posts in blog
			PostsResource postRes = new PostsResource(blogService);

			PostsResource.ListRequest postListReq = postRes.List(blog.Id);
			postListReq.Key = apiKey;

			string firstToken = "";

			List<Post> lstPosts = new List<Post>();

			//Listing all posts
			while (true)
			{
				PostList postLst = postListReq.Execute();
				postListReq.PageToken = postLst.NextPageToken;

				if (firstToken == "")
				{
					firstToken = postLst.NextPageToken;
				}
				else if (firstToken != "" && postLst.NextPageToken == firstToken)
				{
					break;
				}

				for (int i = 0; i < postLst.Items.Count; i++)
				{
					Console.WriteLine("#" + (lstPosts.Count + 1) + "-->");
					Console.WriteLine("Title: " + postLst.Items[i].Title);
					Console.WriteLine("Link: " + postLst.Items[i].Url);
					Console.WriteLine("Post id: " + postLst.Items[i].Id);
					Console.WriteLine();
					lstPosts.Add(postLst.Items[i]);

				}
			}

			Console.WriteLine();
			Console.WriteLine("Total post: " + lstPosts.Count);

			//For updating the blog's title
			
			Console.WriteLine("Press to continue update title...");
			Console.ReadKey();
			for (int i = 0; i < lstPosts.Count; i++)
			{
				Post updatePost = lstPosts[i];
				if (updatePost.Title == ReplaceTitle(updatePost.Title))
				{
					continue;
				}


				Console.WriteLine(updatePost.Title + " -> " + ReplaceTitle(updatePost.Title));
				Console.WriteLine("Continue?(y/n/q)");


				string input = Console.ReadKey().KeyChar.ToString().ToLower();
				if (input == "n")
				{
					continue;
				}
				else if (input == "q")
				{
					break;
				}
				else if (input == "y")
				{
					Console.WriteLine("\nUpdating...");
					//Set a new title
					updatePost.Title = ReplaceTitle(updatePost.Title);
					//Update title
					PostsResource.UpdateRequest blogUpdate = postRes.Update(updatePost, blog.Id, updatePost.Id);
					blogUpdate.Execute();
					Console.WriteLine("Updated...");
				}

			}

			Console.WriteLine("Done");

			Console.ReadKey();

		}
		private static IAuthorizationState getAuth(NativeApplicationClient arg)
		{
			IAuthorizationState state = new AuthorizationState(new[] { BloggerService.Scopes.Blogger.GetStringValue() });
			state.Callback = new Uri(NativeApplicationClient.OutOfBandCallbackUrl);
			Uri authUri = arg.RequestUserAuthorization(state);
			Console.WriteLine("Auth url: " + authUri);

			Process.Start(authUri.ToString());
			Console.WriteLine("Auth code:");
			string authCode = Console.ReadLine();

			return arg.ProcessUserAuthorization(authCode, state);

		}
		private static string ReplaceTitle(string Title)
		{
			return Regex.Replace(Regex.Replace(Regex.Replace(Title, "【", "["), "】", "] "), @"\] \[", "][");
		}
	}
}
