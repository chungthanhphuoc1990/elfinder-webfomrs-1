<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="elFinder.WebTest._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <h2>
        elFinder file manager
    </h2>
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
