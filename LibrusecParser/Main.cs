using System;
using System.Xml;
using System.IO;
using HtmlAgilityPack;
using System.Text;

namespace LibrusecParser
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			//args = new string[] { "--html2text","/home/yrtimid/tmp/9/1.html" };
			//			string dirPath = @"/home/yrtimid/Downloads/gromiko/";
			//			string[] files = Directory.GetFiles(dirPath, "*.html", SearchOption.TopDirectoryOnly);
			//			foreach (string file in files)
			//			{
			//				Console.Write(Path.GetFileName(file)+"\t");
			//				try{
			//					CleanHtml(file);
			//					Console.WriteLine("OK");
			//				}
			//				
			//				catch(Exception ex){
			//					Console.WriteLine(ex.Message);
			//				}
			//			}
			
			if (args.Length == 2 && args[0] == "--html2text")
			{
				if (System.IO.File.Exists (args[1]))
				{
					Console.Write (Path.GetFileName (args[1]) + "\t");
					try
					{
						HtmlToText (args[1]);
						Console.WriteLine ("OK");
					
					}
					catch (Exception ex)
					{
						Console.WriteLine (ex.Message);
					}
				}
				else if (System.IO.Directory.Exists (args[1]))
				{
					string[] files = Directory.GetFiles (args[1], "*.html", SearchOption.TopDirectoryOnly);
					foreach (string file in files)
					{
						Console.Write (Path.GetFileName (file) + "\t");
						try
						{
							HtmlToText (file);
							Console.WriteLine ("OK");
						
						}
						catch (Exception ex)
						{
							Console.WriteLine (ex.Message);
						}
					}
				}
				else
				{
					Console.WriteLine ("Specified file or directory ["+args[1]+"] was not found");
				}
			}
			else
			{
				Console.WriteLine ("Using:");
				Console.WriteLine (System.Reflection.Assembly.GetCallingAssembly ().GetName().Name + " --html2text FILE");
				return 1;
			}
			
			
			/*
			string dirPath = @"/home/yrtimid/Downloads/gromiko/result/";
			string[] files = Directory.GetFiles (dirPath, "*.html", SearchOption.TopDirectoryOnly);
			foreach (string file in files)
			{
				Console.Write (Path.GetFileName (file) + "\t");
				try
				{
					HtmlToText (file);
					Console.WriteLine ("OK");
				
				} catch (Exception ex)
				{
					Console.WriteLine (ex.Message);
				}
			}
			*/
		
			return 0;
		}

		public static void CleanHtml (string filePath)
		{
			string html = System.IO.File.ReadAllText (filePath);
			
			HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument ();
			
			doc.LoadHtml (html);
			
			#region head
			HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes ("/html/head/*");
			foreach (var node in nodes)
			{
				if (node.OriginalName == "meta" || node.OriginalName == "title")
					continue;
				
				node.Remove ();
			}
			#endregion
			
			#region
			HtmlNode body = doc.DocumentNode.SelectSingleNode ("//body");
			HtmlNode main = body.SelectSingleNode ("id('main')");
			if (main != null)
			{
				body.RemoveAll ();
				body.AppendChild (main);
			
			} else
			{
				Console.WriteLine ("no body or main");
			}
			
			string[] idToRemove = new string[] { "content-top", "content-bottom" };
			string[] classToRemove = new string[] { "breadcrumb" };
			
			
			
			foreach (string id in idToRemove)
			{
				HtmlNode n = main.SelectSingleNode ("id('" + id + "')");
				if (n != null)
					n.Remove ();
			}
			
			foreach (string id in classToRemove)
			{
				HtmlNode n = main.SelectSingleNode ("*[@class='" + id + "']");
				if (n != null)
					n.Remove ();
			}
			
			HtmlNode textarea = main.SelectSingleNode ("textarea");
			if (textarea != null)
				textarea.Remove ();
			#endregion
			
			HtmlNode title = main.SelectSingleNode ("h3[@class='book']");
			
			string dir = Path.Combine (Path.GetDirectoryName (filePath), "result");
			string resultPath;
			if (title != null)
			{
				StringBuilder newTitle = new StringBuilder ();
				
				foreach (string line in title.InnerText.Split (new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
				{
					if (line.Trim ().Length > 0)
					{
						newTitle.Append (line.Trim ());
						newTitle.Append (" - ");
					}
				}
				
				resultPath = Path.Combine (dir, newTitle.ToString ().TrimEnd (' ', '-')) + ".html";
			} else
				
				resultPath = Path.Combine (dir, Path.GetFileName (filePath));
			
			doc.Save (resultPath);
		}

		public static void HtmlToText (string filePath)
		{
			string html = System.IO.File.ReadAllText (filePath);
			
			HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument ();
			
			doc.LoadHtml (html);
			
			TextWriter writer = new StreamWriter (Path.Combine (Path.GetDirectoryName (filePath), Path.GetFileNameWithoutExtension (filePath) + ".txt"));
			
			HtmlNode body = doc.DocumentNode.SelectSingleNode ("//body");
			if (body == null)
			{
				Console.Error.WriteLine ("Can't find body");
				return;
			}

			HtmlNode main = body.SelectSingleNode ("id('main')");
			if (main != null)
			{
				HtmlNode title = main.SelectSingleNode ("h3[@class='book']");
				if (title != null)
					writer.Write (title.InnerText);
			}
			if (main == null)
				main = body;
			
			writer.WriteLine (TransformNode (main));
			
			writer.Close ();
			
		}

		public static string TransformNode (HtmlNode node)
		{
			switch (node.Name)
			{
			case "#text":
				if (node.InnerText.Contains ("\t"))
				{
					//Console.WriteLine (node.InnerText);
				}
				return node.InnerText.Trim ('\t', ' ').Replace ("&nbsp;", " ");
			case "p":
			case "i":
			case "b":
			case "small":
			case "span":
				return TransformSubNodes (node);
			case "h5":
			case "h6":
			case "h7":
			case "h8":
				return Environment.NewLine + TransformSubNodes (node) + Environment.NewLine;
			case "h1":
			case "h2":
			case "h3":
			case "h4":
				return Environment.NewLine + Environment.NewLine + TransformSubNodes (node) + Environment.NewLine;
			case "sup":
				return "[" + TransformSubNodes (node) + "]";
			case "img":
				return "";
			case "a":
				return (node.Attributes["title"]!=null?node.Attributes["title"].Value:"") + TransformSubNodes (node);
			case "br":
				return Environment.NewLine;
			case "div":
				return TransformSubNodes (node);
			// + Environment.NewLine;
			case "blockquote":
				
				string[] lines = TransformSubNodes (node).Split (new string[] { Environment.NewLine }, StringSplitOptions.None);
				StringBuilder b = new StringBuilder ();
				foreach (var line in lines)
				{
					b.AppendFormat ("\t{0}{1}", line, Environment.NewLine);
				}

				return b.ToString ();
			case "li":
				return "* " + TransformSubNodes (node);
			default:
				Console.WriteLine (node.Name);
				return TransformSubNodes (node);
			}
		}

		public static string TransformSubNodes (HtmlNode node)
		{
			if (node.ChildNodes.Count > 0)
			{
				StringBuilder b = new StringBuilder ();
				foreach (var cnode in node.ChildNodes)
				{
					b.Append (TransformNode (cnode));
				}
				return b.ToString ();
			} 
			else
			{
				//Console.WriteLine (node.Name);
				return node.InnerHtml.TrimStart ('\t', ' ').Replace ("&nbsp;", " ");
			}
			
		}
	}
	
	
}

