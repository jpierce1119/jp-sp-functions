
using System;
using System.Net;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Utilities;
using PnPAuthenticationManager = OfficeDevPnP.Core.AuthenticationManager;
using Microsoft.SharePoint.Client;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{

    dynamic data = await req.Content.ReadAsAsync<object>();

    string vendorName = data["vendorName"];

    log.Info($"vendorName = '{vendorName}'");
    
    string userName = System.Environment.GetEnvironmentVariable("SharePointUser", EnvironmentVariableTarget.Process);
    string password = System.Environment.GetEnvironmentVariable("SharePointPassword", EnvironmentVariableTarget.Process);
	string sharePointSiteUrl = System.Environment.GetEnvironmentVariable("SharePointSiteUrl", EnvironmentVariableTarget.Process);

	var authenticationManager = new PnPAuthenticationManager();
    var clientContext = authenticationManager.GetSharePointOnlineAuthenticatedContextTenant(sharePointSiteUrl, userName, password);
    var pnpClientContext = PnPClientContext.ConvertFrom(clientContext);

    string newFolderUrl = UrlUtility.Combine(baseFolderServerRelativeUrl, newFolderName);

    string resultMessage = "";
	var success = true;

	var documentLibraryCreated = Create(sharePointSiteUrl, vendorName, userName, password);
	if (documentLibraryCreated) {
		resultMessage = "Document Library Creation successful\r\n";
	}
	else
	{
		resultMessage = "Document Library Creation unsucessful\r\n";
		success = false;
	}

	var itemId = UpdateVendorLinks(sharePointSiteUrl, vendorName, password);
	if (itemId == 0)
	{
		resultMessage += "Vendor Links update unsuccessful\r\n";
		success = false;
	}
	else
	{
		resultMessage += "Vendor Links update successful\r\n";
	}
	
	itemId = UpdateVendorTrackingList(sharePointSiteUrl, vendorName, password);
	if (itemId == 0)
	{
		resultMessage += "Vendor Tracking update unsuccessful\r\n";
		success = false;
	}
	else
	{
		resultMessage += "Vendor Tracking update successful\r\n";
	}

	return success ? req.CreateResponse(HttpStatusCode.OK, resultMessage) : req.CreateResponse(HttpStatusCode.BadRequest, resultMessage);
}

public static bool Create(string sharePointSiteUrl, string documentLibrary, string userName, string password)
{
	var authenticationManager = new PnPAuthenticationManager();
	var clientContext = authenticationManager.GetSharePointOnlineAuthenticatedContextTenant(sharePointSiteUrl, userName, password);
	var pnpClientContext = PnPClientContext.ConvertFrom(clientContext);
	try
	{
		var docLibrary = pnpClientContext.Web.Lists.GetByTitle(documentLibrary);
		pnpClientContext.Load(docLibrary);
		pnpClientContext.ExecuteQuery();

		return false;
	}
	catch
	{
		var list = pnpClientContext.Web.CreateList(ListTemplateType.DocumentLibrary, documentLibrary, false);
		pnpClientContext.Load(list);
		pnpClientContext.ExecuteQuery();

		return true;
	}
}

public static int UpdateVendorTrackingList(string siteUrl, string library, string userName, string password)
{
	var authenticationManager = new PnPAuthenticationManager();
	var clientContext = authenticationManager.GetSharePointOnlineAuthenticatedContextTenant(siteUrl, userName, password);
	var context = PnPClientContext.ConvertFrom(clientContext);

	var documentLibrary = context.Web.Lists.GetByTitle(library);
	context.Load(documentLibrary);
	context.ExecuteQuery();

	var rootFolder = documentLibrary.RootFolder;
	context.Load(rootFolder);
	context.ExecuteQuery();

	var url = rootFolder.ServerRelativeUrl;

	var list = context.Web.Lists.GetByTitle("Vendor Tracking");
	context.Load(list);
	context.ExecuteQuery();

	var query = new CamlQuery();
	query.ViewXml = @"<View><Query><Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + library + "</Value></Eq></Where></Query><RowLimit>100</RowLimit></View>";

	var listItems = list.GetItems(query);
	context.Load(listItems);
	context.ExecuteQuery();

	if (listItems.Count == 1)
	{
		foreach (var listItem in listItems)
		{
			context.Load(listItem);
			context.ExecuteQuery();
			var itemId = listItem.Id;
			var listItemUrl = new FieldUrlValue();
			listItemUrl.Url = url;
			listItemUrl.Description = "Click Here";

			listItem["Document_x0020_Access_x0020_Link"] = listItemUrl;
			listItem.Update();
			list.Update();
			context.ExecuteQuery();
			return itemId;
		}
	}
	return 0;
}

public static bool UpdateVendorLinks(string siteUrl, string library, string userName, string password)
{
	var authenticationManager = new PnPAuthenticationManager();
	var clientContext = authenticationManager.GetSharePointOnlineAuthenticatedContextTenant(siteUrl, userName, password);
	var context = PnPClientContext.ConvertFrom(clientContext);

	var documentLibrary = context.Web.Lists.GetByTitle(library);
	context.Load(documentLibrary);
	context.ExecuteQuery();

	var rootFolder = documentLibrary.RootFolder;
	context.Load(rootFolder);
	context.ExecuteQuery();

	var url = rootFolder.ServerRelativeUrl;

	var list = context.Web.Lists.GetByTitle("Vendor Links");
	context.Load(list);
	context.ExecuteQuery();

	var query = new CamlQuery();
	query.ViewXml = @"<View><Query><Where><Eq><FieldRef Name='Vendor_x0020_Name'/><Value Type='Lookup'>" + library + "</Value></Eq></Where></Query><RowLimit>100</RowLimit></View>";

	var listItems = list.GetItems(query);
	context.Load(listItems);
	context.ExecuteQuery();

	if (listItems.Count == 1)
	{
		foreach (var listItem in listItems)
		{
			context.Load(listItem);
			context.ExecuteQuery();

			var listItemUrl = new FieldUrlValue
			{
				Url = url,
				Description = library
			};

			listItem["URL"] = listItemUrl;
			listItem.Update();
			list.Update();
			context.ExecuteQuery();
		}

		return true;
	}
	return false;
}