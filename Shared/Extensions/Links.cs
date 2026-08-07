public static class Links
{
    
    public static List<HyperLink> GenerateLinks(HttpContext httpContext, Guid id, string prefixo)
    {
        // var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";        
        var baseUrl = BaseUrl(httpContext);
        var links = new List<HyperLink>
        {
            new("self", $"{baseUrl}/{prefixo}/{id}", "GET"),
            new("collection", $"{baseUrl}/{prefixo}", "GET"),
            new("update", $"{baseUrl}/{prefixo}/{id}", "PUT"),
            new("delete", $"{baseUrl}/{prefixo}/{id}", "DELETE")
        };

        return links;
    }    

    public static string BaseUrl(HttpContext httpContext)
    {
        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    }
    
}
