<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="YouTubePreview.aspx.cs" Inherits="Dev080701.layouts.YouTubePreview" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title></title>
</head>
<body>
  <form id="form1" runat="server">
  <div>
    <object width="460" height="300">
      <param name="movie" value="<%# GetUrl() %>" />
      <param name="allowFullScreen" value="false" />
      <param name="allowscriptaccess" value="always" />
      <embed src="<%= GetUrl() %>" type="<%= GetMimeType() %>"
        allowscriptaccess="always" allowfullscreen="false" width="460" height="300"></embed>
    </object>
  </div>
  </form>
</body>
</html>
