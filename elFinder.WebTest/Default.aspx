﻿<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="elFinder.WebTest._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <h2>
        elFinder file manager
    </h2>

    <p><b>Note:</b> be sure to have a symbolic link named <code>data</code> that points to <code>c:\users</code> directory.</p>
    <p>elFinder connector is by default configured to search <code>data</code> directory under the website directory 
    (see <code>localFSRootDirectoryPath</code> and <code>baseUrl</code> properties of the <code>elFinder</code> Web.config section).</p>
    <p>To create a symbolic link, open command prompt in website directory (where the Web.config is) and type: <code>mklink /D data c:\users</code></p>

   <div class="fileManager">finder</div>   

    <script type="text/javascript" charset="utf-8">
        $(function () {
            $('.fileManager').elfinder({
                url: '/elfinder.connector',
                height: 600
            });
        });
    </script>
</asp:Content>
