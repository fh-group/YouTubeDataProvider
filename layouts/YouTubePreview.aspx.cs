using System;
using Sitecore.Data.Items;
using Sitecore.Web;
using Sitecore.Sites;
using Sitecore.Configuration;
using Sitecore.Globalization;

namespace Dev080701.layouts
{
   public partial class YouTubePreview : System.Web.UI.Page
   {
      private string url;
      private string type;

      protected void Page_Load(object sender, EventArgs e)
      {
         using (new SiteContextSwitcher(Factory.GetSite("shell")))
         {
            if (!IsPostBack || 1==1)
            {
               string id = WebUtil.GetQueryString("id");
               string ver = WebUtil.GetQueryString("vs");
               string lang = WebUtil.GetQueryString("la");
               Item item =
                  Sitecore.Context.ContentDatabase.Items[
                     Sitecore.Data.ID.Parse(id), Language.Parse(lang), Sitecore.Data.Version.Parse(ver)];
               if (item != null)
               {
                  url = item["url"];
                  type = item["mime type"];
                  Page.DataBind();

               }
            }
         }
      }

      public string GetUrl()
      {
         return url;
      }

      public string GetMimeType()
      {
         return type;
      }
   }
}
