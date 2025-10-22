using System.Web.Routing;

namespace organic_store.Helpers
{
    public static class RouteValueExtensions
    {
        public static RouteValueDictionary Merge(this object original, object extra)
        {
            var dict = new RouteValueDictionary(original);
            foreach (var prop in extra.GetType().GetProperties())
            {
                dict[prop.Name] = prop.GetValue(extra);
            }
            return dict;
        }
    }
}
