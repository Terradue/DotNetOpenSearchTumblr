//
//  TumblrFeed.cs
//
//  Author:
//       Enguerran Boissier <enguerran.boissier@terradue.com>
//
//  Copyright (c) 2014 Terradue


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using System.Xml;
using ServiceStack.Text;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.ServiceModel.Syndication;

namespace Terradue.OpenSearch.Tumblr {

    public class TumblrApplication {

        /// <summary>
        /// Gets or sets the Tumblr Api key.
        /// </summary>
        /// <value>The consumer key.</value>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the API method used.
        /// </summary>
        /// <value>The API method.</value>
        public string ApiMethod { get; set; }

        /// <summary>
        /// Gets or sets the type of the API.
        /// Specify one of the following:  text, quote, link, answer, video, audio, photo, chat
        /// </summary>
        /// <value>The type of the API.</value>
        public string ApiType { get; set; }

        /// <summary>
        /// Gets or sets the tumblr API base URL.
        /// </summary>
        /// <value>The tumblr API base URL.</value>
        public string ApiBaseUrl { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.OpenSearch.Tumblr.TumblrApplication"/> class.
        /// </summary>
        /// <param name="apiKey">API key.</param>
        public TumblrApplication(string apiKey){
            this.ApiKey = apiKey;
            this.ApiMethod = "posts"; //Retrieve Published Posts
            this.ApiType = "text";
            this.ApiBaseUrl = "http://api.tumblr.com/v2/blog";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.OpenSearch.Tumblr.TumblrApplication"/> class.
        /// </summary>
        /// <param name="apikey">Api key.</param>
        /// <param name="apimethod">Api method.</param>
        /// <param name="apitype">Api type.</param>
        /// <param name="apibaseurl">Api baseurl.</param>
        public TumblrApplication(string apikey, string apimethod, string apitype, string apibaseurl){
            this.ApiKey = apikey;
            this.ApiMethod = apimethod;
            this.ApiType = apitype;
            this.ApiBaseUrl = apibaseurl;
        }

    }

    public class TumblrFeed : IOpenSearchable {

        /// <summary>
        /// Get the local identifier.
        /// </summary>
        /// <value>The local identifier of the OpenSearchable entity.</value>
        public string Identifier { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the abstract.
        /// </summary>
        /// <value>The abstract.</value>
        public string Abstract { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>The content.</value>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        /// <value>The time.</value>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        /// <value>The author.</value>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public string Tags { get; set; }
    
        /// <summary>
        /// Gets or sets the application.
        /// </summary>
        /// <value>The application containing all keys.</value>
        protected TumblrApplication Application { get; set; } 

        /// <summary>
        /// Gets or sets the base URL.
        /// </summary>
        /// <value>The base URL.</value>
        protected string BaseUrl { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.OpenSearch.Tumblr.TumblrFeed"/> class.
        /// </summary>
        /// <param name="BaseUrl">Base URL.</param>
        public TumblrFeed(string BaseUrl) {
            this.BaseUrl = BaseUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.OpenSearch.Tumblr.TumblrFeed"/> class.
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="BaseUrl">Base URL.</param>
        public TumblrFeed(TumblrApplication app, string BaseUrl) {
            this.Application = app;
            this.BaseUrl = BaseUrl;
        }

        /// <summary>
        /// Gets the list of feeds.
        /// </summary>
        /// <returns>The feeds.</returns>
        public List<TumblrFeed> GetFeeds(){
            string blogName = this.Author;

            List<TumblrFeed> result = new List<TumblrFeed>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Application.ApiBaseUrl+"/"+blogName+".tumblr.com/"+Application.ApiMethod+"?api_key="+Application.ApiKey);
            request.Method = "GET";
            request.ContentType = "application/json"; 

            StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream());
            string text = reader.ReadToEnd();

            TumblrResponse response = JsonSerializer.DeserializeFromString<TumblrResponse>(text);
            foreach(TumblrResponsePost post in response.response.posts){
                TumblrFeed news = new TumblrFeed(this.BaseUrl);
                news.Identifier = post.id.ToString();
                news.Title = post.title;
                news.Abstract = (post.caption != null ? post.caption : post.body);
                news.Url = post.short_url;
                news.Author = post.blog_name;
                news.Time = post.date;
                news.Tags = String.Join(",", post.tags);
                news.Content = post.type;

                result.Add(news);
            }
            return result;
        }

        /// <summary>
        /// Generates the atom feed list.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <param name="parameters">Parameters.</param>
        void GenerateAtomFeed(Stream input, System.Collections.Specialized.NameValueCollection parameters) {

            string blogName = this.Author;

            AtomFeed feed = new AtomFeed();
            List<AtomItem> items = new List<AtomItem>();

            int count = Int32.Parse(parameters["count"] != null ? parameters["count"] : "20");
            int offset = Int32.Parse(parameters["startIndex"] != null ? parameters["startIndex"] : "0");
            string xq = parameters["q"] + (this.Tags != null && this.Tags != string.Empty ? "," + this.Tags : "");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Application.ApiBaseUrl+"/"+blogName+".tumblr.com/"+Application.ApiMethod+"/"+Application.ApiType
                                                                       +"?api_key="+Application.ApiKey+"&limit="+count+"&offset="+offset);

            request.Method = "GET";
            request.ContentType = "application/json"; 

            StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream());
            string text = reader.ReadToEnd();
            TumblrResponse response = JsonSerializer.DeserializeFromString<TumblrResponse>(text);

            foreach(TumblrResponsePost post in response.response.posts){
                DateTimeOffset time = new DateTimeOffset(post.date);
                AtomItem item = new AtomItem(post.title, post.body, new Uri(post.short_url), post.id.ToString(), time);
                item.PublishDate = time;
                item.Categories.Add(new SyndicationCategory(post.type));
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

        /// <summary>
        /// Gets the query settings.
        /// </summary>
        /// <returns>The query settings.</returns>
        /// <param name="ose">Ose.</param>
        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null)
                return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        /// <summary>
        /// Gets the default MIME-type that the entity can be searched for
        /// </summary>
        /// <value>The default MIME-type.</value>
        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

        /// <summary>
        /// Create the OpenSearch Request for the requested mime-type the specified type and parameters.
        /// </summary>
        /// <param name="mimetype">Mime-Type requested to the OpenSearchable entity</param>
        /// <param name="parameters">Parameters of the request</param>
        public OpenSearchRequest Create(string mimetype, System.Collections.Specialized.NameValueCollection parameters) {
            UriBuilder url = new UriBuilder(this.BaseUrl);
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

        /// <summary>
        /// Get the entity's OpenSearchDescription.
        /// </summary>
        /// <returns>The OpenSearchDescription describing the IOpenSearchable.</returns>
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
            OSDD.Description = "This Search Service performs queries in the available tumblr feeds. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            // The new URL template list 
            Hashtable newUrls = new Hashtable();
            UriBuilder urib;
            NameValueCollection query = new NameValueCollection();
            string[] queryString;

            urib = new UriBuilder(this.BaseUrl);
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

        /// <summary>
        /// Gets the OpenSearch parameters for a given Mime-Type.
        /// </summary>
        /// <returns>OpenSearch parameters NameValueCollection.</returns>
        /// <param name="mimeType">MIME type for the requested parameters</param>
        public System.Collections.Specialized.NameValueCollection GetOpenSearchParameters(string mimeType) {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }

        /// <summary>
        /// Get the total of possible results for the OpenSearchable entity
        /// </summary>
        /// <returns>a unsigned long number representing the number of items searchable</returns>
        public long TotalResults {
            get { return 0; }
        }

        /// <summary>
        /// Gets the search base URL.
        /// </summary>
        /// <returns>The search base URL.</returns>
        /// <param name="mimeType">MIME type.</param>
        public OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl (string.Format("{0}/{1}/search", this.BaseUrl, "blog"));
        }

        /// <summary>
        /// Optional function that apply to the result after the search and before the result is returned by OpenSearchEngine.
        /// </summary>
        /// <param name="osr">IOpenSearchResult cotnaing the result of the a search</param>
        /// <param name="request">Request.</param>
        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr) {}

        public ParametersResult DescribeParameters() {
            return OpenSearchFactory.GetDefaultParametersResult();
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
        public string caption {get; set; }
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

