using System;
using System.Web;
using System.Net;
using System.IO;
using System.Text;
using ServiceStack.Text;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Terradue.Portal;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Schema;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.Linq;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Engine;
using Terradue.ServiceModel.Syndication;

namespace Terradue.OpenSearch.Controller {
    public class TumblrNews : Article, IOpenSearchable {
    
        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.TepQW.Controller.TumblrNews"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public TumblrNews(IfyContext context) : base(context) {}

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public new static TumblrNews FromId(IfyContext context, int id){
            return (TumblrNews)Article.FromId(context, id);
        }

        public List<TumblrNews> GetFeeds(){
            string blogName = this.Author;
            string tumblrBaseUrl = "http://api.tumblr.com/v2/blog";
            string api = "posts";
            string type = "text";
            string apiKey = context.GetConfigValue("Tumblr-apikey");

            List<TumblrNews> result = new List<TumblrNews>();

            //var client = new JsonServiceClient(tumblrBaseUrl+"/"+blogName+".tumblr.com/posts/text?api_key=5dmloJh2jq9ldxN9nFZdA477Kb4XrwtZQR3hLsjl0eW2HlsS0N");
            //var response = client.Get(new TumblrRequest ());

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tumblrBaseUrl+"/"+blogName+".tumblr.com/"+api+"/"+type+"?api_key="+apiKey);
            request.Method = "GET";
            request.ContentType = "application/json"; 

            StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream());
            string text = reader.ReadToEnd();

            TumblrResponse response = JsonSerializer.DeserializeFromString<TumblrResponse>(text);
            foreach(TumblrResponsePost post in response.response.posts){
                TumblrNews news = new TumblrNews(context);
                news.Id = 0;
                news.Identifier = post.id.ToString();
                news.Title = post.title;
                news.Abstract = post.body;
                news.Url = post.short_url;
                news.Author = post.blog_name;
                news.Time = post.date;
                news.Tags = String.Join(",", post.tags);

                result.Add(news);
            }
            return result;
        }

        void GenerateAtomFeed(Stream input, System.Collections.Specialized.NameValueCollection parameters) {

            string blogName = this.Author;
            string tumblrBaseUrl = "http://api.tumblr.com/v2/blog";
            string api = "posts";
            string type = "text";
            string apiKey = context.GetConfigValue("Tumblr-apikey");

            AtomFeed feed = new AtomFeed();
            List<AtomItem> items = new List<AtomItem>();

            int count = Int32.Parse(parameters["count"] != null ? parameters["count"] : "20");
            int offset = Int32.Parse(parameters["startIndex"] != null ? parameters["startIndex"] : "0");
            string q = parameters["q"] + (this.Tags != null && this.Tags != string.Empty ? "," + this.Tags : "");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tumblrBaseUrl+"/"+blogName+".tumblr.com/"+api+"/"+type
                                                                       +"?api_key="+apiKey
                                                                       +"&limit="+count
                                                                       +"&offset="+offset);
            request.Method = "GET";
            request.ContentType = "application/json"; 

            StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream());
            string text = reader.ReadToEnd();
            TumblrResponse response = JsonSerializer.DeserializeFromString<TumblrResponse>(text);

            foreach(TumblrResponsePost post in response.response.posts){
                DateTimeOffset time = new DateTimeOffset(post.date);
                AtomItem item = new AtomItem(post.title, post.body, new Uri(post.short_url), post.id.ToString(), time);
                items.Add(item);
            }

            feed.Items = items;

            var sw = XmlWriter.Create(input);
            Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter(feed.Feed);
            atomFormatter.WriteTo(sw);
            sw.Flush();
            sw.Close();

            return;

        }

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByDiscoveryContentType(this.DefaultMimeType);
            if (osee == null)
                return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

        public OpenSearchRequest Create(string mimetype, System.Collections.Specialized.NameValueCollection parameters) {
            UriBuilder url = new UriBuilder(context.BaseUrl);
            url.Path += "/"+this.Identifier+"/";
            var array = (from key in parameters.AllKeys
                         from value in parameters.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            url.Query = string.Join("&", array);

            MemoryOpenSearchRequest request = new MemoryOpenSearchRequest(new OpenSearchUrl(url.ToString()), mimetype);

            Stream input = request.MemoryInputStream;

            GenerateAtomFeed(input, parameters);

            return request;
        }

        public Terradue.OpenSearch.Schema.OpenSearchDescription GetOpenSearchDescription() {
            OpenSearchDescription OSDD = new OpenSearchDescription();

            OSDD.ShortName = "Terradue Catalogue";
            OSDD.Attribution = "European Space Agency";
            OSDD.Contact = "info@esa.int";
            OSDD.Developer = "Terradue GeoSpatial Development Team";
            OSDD.SyndicationRight = "open";
            OSDD.AdultContent = "false";
            OSDD.Language = "en-us";
            OSDD.OutputEncoding = "UTF-8";
            OSDD.InputEncoding = "UTF-8";
            OSDD.Description = "This Search Service performs queries in the available services of Tep QuickWin. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            // The new URL template list 
            Hashtable newUrls = new Hashtable();
            UriBuilder urib;
            NameValueCollection query = new NameValueCollection();
            string[] queryString;

            urib = new UriBuilder(context.BaseUrl);
            urib.Path = String.Format("/{0}/search",this.Identifier);
            query.Add(this.GetOpenSearchParameters("application/atom+xml"));

            query.Set("format", "atom");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("application/atom+xml", new OpenSearchDescriptionUrl("application/atom+xml", urib.ToString(), "search"));

            query.Set("format", "json");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("application/json", new OpenSearchDescriptionUrl("application/json", urib.ToString(), "search"));

            query.Set("format", "html");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("text/html", new OpenSearchDescriptionUrl("application/html", urib.ToString(), "search"));

            OSDD.Url = new OpenSearchDescriptionUrl[newUrls.Count];

            newUrls.Values.CopyTo(OSDD.Url, 0);

            return OSDD;
        }

        public System.Collections.Specialized.NameValueCollection GetOpenSearchParameters(string mimeType) {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }

        public ulong TotalResults() {
            return 0;
        }

        public void ApplyResultFilters(ref IOpenSearchResult osr) {}

        public OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl (string.Format("{0}/{1}/search", context.BaseUrl, "blog"));
        }
    }

    [DataContract]
    public class TumblrResponse {
        [DataMember]
        public TumblrResponseMeta meta { get; set; }
        [DataMember]
        public TumblrResponseResponse response { get; set; }
    }

    [DataContract]
    public class TumblrResponseMeta{
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public string msg { get; set; }
    }

    [DataContract]
    public class TumblrResponseResponse{
        [DataMember]
        public TumblrResponseBlog blog {get; set; }
        [DataMember]
        public List<TumblrResponsePost> posts {get; set; }
        [DataMember]
        public int total_posts {get; set; }
    }

    [DataContract]
    public class TumblrResponseBlog{
        [DataMember]
        public string title {get; set; }
        [DataMember]
        public string name {get; set; }
        [DataMember]
        public int posts {get; set; }
        [DataMember]
        public string url {get; set; }
        [DataMember]
        public DateTime updated {get; set; }
        [DataMember]
        public string description {get; set; }
        [DataMember]
        public string ask {get; set; }
        [DataMember]
        public string ask_page_title {get; set; }
        [DataMember]
        public bool ask_anon {get; set; }
        [DataMember]
        public bool is_nsfw {get; set; }
        [DataMember]
        public bool share_likes {get; set; }
        [DataMember]
        public int likes {get; set; }
    }

    [DataContract]
    public class TumblrResponsePost{
        [DataMember]
        public string blog_name {get; set; }
        [DataMember]
        public long id {get; set; }
        [DataMember]
        public string post_url {get; set; }
        [DataMember]
        public string slug {get; set; }
        [DataMember]
        public string type {get; set; }
        [DataMember]
        public DateTime date {get; set; }
        [DataMember]
        public long timestamp {get; set; }
        [DataMember]
        public string state {get; set; }
        [DataMember]
        public string format {get; set; }
        [DataMember]
        public string reblog_key {get; set; }
        [DataMember]
        public List<string> tags {get; set; }
        [DataMember]
        public string short_url {get; set; }
        [DataMember]
        public List<string> highlighted {get; set; }
        [DataMember]
        public int note_count {get; set; }
        [DataMember]
        public string source_url {get; set; }
        [DataMember]
        public string source_title {get; set; }
        [DataMember]
        public string title {get; set; }
        [DataMember]
        public string body {get; set; }
    }
}

