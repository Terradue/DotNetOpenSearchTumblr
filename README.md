# DotNetOpenSearchTumblr - .Net Library to Access Tumblr API via OpenSearch

DotNetOpenSearchTumblr is a library targeting .NET 4.0 and above providing an easy way to perform opensearch requests on the Tumblr API

## Usage examples

```c#
HttpRequest httpRequest = HttpContext.Current.Request;
OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
Type type = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
List<TumblrFeed> tumblrs = TumblrNews.LoadTumblrFeeds(context);
MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(tumblrs.Cast<IOpenSearchable>().ToList(), ose);
result = ose.Query(multiOSE, httpRequest.QueryString, type);
```

## Supported Platforms

* .NET 4.0 (Desktop / Server)
* Xamarin.iOS / Xamarin.Android / Xamarin.Mac
* Mono 2.10+

## Build

DotNetOpenSearchTumblr is a single assembly designed to be easily deployed anywhere. 

To compile it yourself, you’ll need:

* Visual Studio 2012 or later, or Xamarin Studio

To clone it locally click the "Clone in Desktop" button above or run the 
following git commands.

```
git clone git@github.com:Terradue/Terradue.OpenSearch.Tumblr.git Terradue.OpenSearch.Tumblr
```

## Copyright and License

Copyright (c) 2014 Terradue

Licensed under the [GPL v3 License](https://github.com/Terradue/DotNetOpenSearchTumblr/blob/master/LICENSE)

## Questions, bugs, and suggestions

Please file any bugs or questions as [issues](https://github.com/Terradue/DotNetOpenSearchTumblr/issues/new) 

## Want to contribute?

Fork the repository [here](https://github.com/Terradue/DotNetOpenSearchTumblr/fork) and send us pull requests.
