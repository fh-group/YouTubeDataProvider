using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Caching;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data.DataProviders;
using Sitecore.Data.IDTables;
using Sitecore.Data.Items;
using Google.GData.YouTube;
using System.Collections;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Reflection;

namespace Sitecore.Data.YouTube
{
  /// <summary>
  /// YouTube read-only data provider
  /// </summary>
  public class YouTubeDataProvider : DataProvider
  {
    #region Variables

    private string rootTemplateID;
    private string resourceTemplateID;
    private string videoOwnerFieldName;
    private string contentDatabase;

    private Hashtable _items;

    private readonly string prefix;

    #endregion Variables

    #region ctor

    public YouTubeDataProvider(string rootTID, string resourceTID, string videoOwnerField, string contentDB)
    {
      prefix = ToString();
      rootTemplateID = rootTID;
      resourceTemplateID = resourceTID;
      contentDatabase = contentDB;
      videoOwnerFieldName = videoOwnerField;
      _items = new Hashtable();
    }

    #endregion ctor

    #region Provider Methods

    // Returns an item descriptor for the item associated with the given item ID. 
    public override ItemDefinition GetItemDefinition(ID itemId, CallContext context)
    {
      ItemDefinition itemDef = null;
      string itemName = string.Empty;
      if (CanProcessYouTubeItem(itemId, context))
      {
        string originalID = GetOriginalRecordID(itemId);
        YTItemInfo itemInfo = GetYTItemInfo(itemId);
        if (itemInfo == null)
        {
          YouTubeFeed ytFeed = GetUserVideos(GetParentID(itemId));
          var entry = ytFeed.Entries.Cast<YouTubeEntry>().Where(item => item.Id.Uri.Content == originalID);
          var firstEntry = entry.FirstOrDefault();
          if (firstEntry != null)
          {
            itemName = ItemUtil.ProposeValidItemName(entry.First().Title.Text);

            itemDef = new ItemDefinition(itemId, itemName, ContentDB.Templates[new ID(resourceTemplateID)].ID, ID.Null);
            _items.Add(itemId, new YTItemInfo(itemId, ID.Parse(resourceTemplateID), itemName, firstEntry));
          }
        }
        else
        {
          itemDef = new ItemDefinition(itemId, itemInfo.Name, itemInfo.TemplateID, ID.Null);
        }
        try
        {
          ItemCache itemCache = CacheManager.GetItemCache(context.DataManager.Database);
          if (itemCache != null)
          {
            ReflectionUtil.CallMethod(itemCache, "RemoveItem", true, false, false, new object[] { itemDef.ID });
          }
        }
        catch (Exception exception)
        {
          Log.Error("Can't clear cache for the YouTube item", exception, this);
        }
        // Do not cache items for this data provider
        if (itemDef != null) ((ICacheable)itemDef).Cacheable = false;
      }
      return itemDef;
    }

    // Returns the specified item’s fields filled with values from the data source. 
    public override FieldList GetItemFields(ItemDefinition itemDefinition, VersionUri versionUri, CallContext context)
    {
      FieldList fields = new FieldList();
      if (CanProcessYouTubeItem(itemDefinition.ID, context))
      {
        Template template = TemplateManager.GetTemplate(resourceTemplateID, ContentDB);
        if (template != null)
        {
          YTItemInfo ytItemInfo = GetYTItemInfo(itemDefinition.ID);
          if (ytItemInfo != null)
          {
            foreach (var field in GetDataFields(template))
            {
              fields.Add(field.ID, GetFieldValue(field, ytItemInfo));
            }
          }
        }
      }
      return fields;
    }

    // Returns the IDs of the given item’s children.
    public override Sitecore.Collections.IDList GetChildIDs(ItemDefinition itemDefinition, CallContext context)
    {
      if (CanProcessParent(itemDefinition.ID, context))
      {

        ID parentID = itemDefinition.ID;
        IDictionary<string, string> ids = LoadDataID(parentID);
        IDList idList = new IDList();

        foreach (var itemId in ids)
        {
          IDTableEntry idEntry = IDTable.GetID(prefix, itemId.Key);
          ID newID;
          if (idEntry == null)
          {
            newID = ID.NewID;
            IDTable.Add(prefix, itemId.Key, newID, parentID, itemId.Value);
          }
          else
          {
            newID = idEntry.ID;
          }
          idList.Add(newID);
        }
        context.DataManager.Database.Caches.DataCache.RemoveItemInformation(itemDefinition.ID);
        return idList;
      }
      return null;
    }

    // Returns the ID of the given item’s parent. 
    public override ID GetParentID(ItemDefinition itemDefinition, CallContext context)
    {
      if (CanProcessYouTubeItem(itemDefinition.ID, context))
      {
        context.Abort();
        return GetParentID(itemDefinition.ID);
      }
      return base.GetParentID(itemDefinition, context);
    }

    // METHOD IS NOT IN USE AS THE DATA PROVIDER IS READ-ONLY
    // Creates an item with the given ID and name, based on the specified template, as a child of the given parent.  
    // Returns True if successful, False otherwise. 
    public override bool CreateItem(ID itemID, string itemName, ID templateID, ItemDefinition parent, CallContext context)
    {
      return false;

    }

    // METHOD IS NOT IN USE AS THE DATA PROVIDER IS READ-ONLY
    // Saves changes made to an item to the physical storage.  
    // Returns True if the item was saved successfully, False otherwise.
    public override bool SaveItem(ItemDefinition itemDefinition, Sitecore.Data.Items.ItemChanges changes, CallContext context)
    {
      return false;
    }

    // METHOD IS NOT IN USE AS THE DATA PROVIDER IS READ-ONLY
    // Removes the specified item from the physical storage.  
    // Returns True if the item was remove successfully, False otherwise.

    // You have to implement this method to clear IDTable entries related to parent item of YouTube videos
    // when you delete that item.
    // Otherwise you will run into an issue when you delete parent item and create another one to store the same YouTube videos.
    // In this case you won't get content of those YouTube items.
    public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
    {
      return false;
    }

    // Should return null in order not to add duplicated languages to common result.
    public override Sitecore.Collections.LanguageCollection GetLanguages(CallContext context)
    {
      return (LanguageCollection)null;
    }

    // This method must return first version for every language to make info appear in content editor.
    public override VersionUriList GetItemVersions(ItemDefinition itemDefinition, CallContext context)
    {
      if (CanProcessYouTubeItem(itemDefinition.ID, context))
      {
        VersionUriList versionUriList = new VersionUriList();
        foreach (var language in LanguageManager.GetLanguages(ContentDB))
        {
          versionUriList.Add(language, Version.First);
        }
        context.Abort();
        return versionUriList;
      }
      return base.GetItemVersions(itemDefinition, context);
    }

    // Gets YouTube items for publishing. It happens all the time.
    public override IDList GetPublishQueue(DateTime from, DateTime to, CallContext context)
    {
      IDList list = new IDList();
      foreach (DictionaryEntry item in _items)
      {
        YTItemInfo ytItemInfo = (YTItemInfo)item.Value;
        list.Add(ytItemInfo.ItemID);
      }
      return list;
    }

    #endregion Provider Methods

    #region private scope

    bool CanProcessYouTubeItem(ID id, CallContext context)
    {
      if (IDTable.GetKeys(prefix, id).Length > 0)
      {
        return true;
      }
      return false;
    }

    string GetOriginalRecordID(ID id)
    {
      IDTableEntry[] idEntries = IDTable.GetKeys(prefix, id);
      if (idEntries != null && idEntries.Length > 0)
      {
        return idEntries[0].Key;
      }
      return null;
    }

    ID GetParentID(ID id)
    {
      IDTableEntry[] idEntries = IDTable.GetKeys(prefix, id);
      if (idEntries != null && idEntries.Length > 0)
      {
        return idEntries[0].ParentID;
      }
      return null;
    }

    // Filters template fields to data fields only (excludes fields of a StandardTemplate data template).
    protected virtual IEnumerable<TemplateField> GetDataFields(Template template)
    {
      return template.GetFields().Where(ItemUtil.IsDataField);
    }

    private string GetFieldValue(TemplateField field, YTItemInfo itemInfo)
    {
      string val = string.Empty;
      switch (field.Name)
      {
        case "Url":
          val = itemInfo.YouTubeItem.Media.Content.Url;
          break;
        case "Id":
          val = itemInfo.YouTubeItem.Media.VideoId.Value;
          break;
        case "Width":
          val = itemInfo.YouTubeItem.Media.Content.Width;
          break;
        case "Height":
          val = itemInfo.YouTubeItem.Media.Content.Height;
          break;
        case "Title":
          val = itemInfo.YouTubeItem.Media.Title.Value;
          break;
        case "Keywords":
          val = itemInfo.YouTubeItem.Media.Keywords.Value;
          break;
        case "Description":
          val = itemInfo.YouTubeItem.Media.Description.Value;
          break;
        case "Extension":
          val = "flv";
          break;
        case "Mime Type":
          val = itemInfo.YouTubeItem.Media.Content.Type;
          break;
        case "Size":
          //val = itemInfo.YouTubeItem.Media.;
          break;
        case "Format":
          break;
        case "Dimensions":
          break;
      }
      if (val == null) { val = string.Empty; }
      return val;
    }

    // Get item from cache
    private YTItemInfo GetYTItemInfo(ID itemID)
    {
      YTItemInfo item = null;
      item = _items[itemID] as YTItemInfo;
      return item;
    }

    bool CanProcessParent(ID id, CallContext context)
    {
      Item item = ContentDB.Items[id];
      bool canProduce = false;
      if (item != null && (ID.Parse(rootTemplateID) == item.TemplateID))
      {
        canProduce = true;
      }
      return canProduce;
    }

    Database ContentDB
    {
      get
      {
        return Factory.GetDatabase(contentDatabase);
      }
    }

    private IDictionary<string, string> LoadDataID(ID parentID)
    {
      YouTubeFeed ytFeed = GetUserVideos(parentID);
      IDictionary<string, string> ids = new Dictionary<string, string>();
      if (ytFeed != null)
      {
        foreach (var ytEntry in ytFeed.Entries)
        {
          // Run a check to make sure that returned results do not contain any restricted video feeds.
          // Restricted feed has no <media:content> tag.
          if (!IsRestrictedVideo(ytEntry))
          {
            ids.Add(ytEntry.Id.Uri.Content, ytEntry.Title.Text);
          }
        }
      }
      return ids;
    }

    /// <summary>
    /// Indicates if a video is a restricted one.
    /// </summary>
    /// <param name="ytEntry"></param>
    /// <returns></returns>
    private bool IsRestrictedVideo(Google.GData.Client.AtomEntry ytEntry)
    {
      return ((YouTubeEntry)ytEntry).Media.Content == null;
    }

    #endregion private scope

    #region YouTube helpers

    private YouTubeFeed GetUserVideos(ID id)
    {
      YouTubeFeed ytFeed = null;
      ID parentID = id;
      if (parentID != (ID)null)
      {
        string authorName = GetAuthorName(parentID);
        if (!string.IsNullOrEmpty(authorName))
        {
          try
          {
            YouTubeService ytService = new YouTubeService(GoogleDevKey);
            // Retrieve only video entries from YouTube service.
            YouTubeQuery ytQuery = new YouTubeQuery(YouTubeQuery.DefaultVideoUri);
            ytQuery.Author = authorName;
            // Set this property to Strict or Moderate to make sure that restricted feeds do not make it into result feed.
            // Results could also be restricted by setting up Restriction property to an IP address. Only allowed feeds will be retrieved for that IP address. 
            // By default this property is set to caller's IP address.
            ytQuery.SafeSearch = YouTubeQuery.SafeSearchValues.Strict;
            ytQuery.NumberToRetrieve = MaxResultsCount;
            ytFeed = ytService.Query(ytQuery);
          }
          catch (Exception ex)
          {
            Log.Error(ex.Message, this);
          }
        }
      }
      return ytFeed;
    }

    private string GetAuthorName(ID parentID)
    {
      Item item = ContentDB.Items[parentID];
      string authorName = string.Empty;
      if (item != null)
      {
        authorName = item[videoOwnerFieldName];
      }
      return authorName;
    }

    [Obsolete("This property was depreated due to changes in Google.Data.API. Client ID is no longer required to query YouTube feeds.")]
    public string GoogleClientID
    {
      get
      {
        return Settings.GetSetting("YouTube.ClientID");
      }
    }

    public string GoogleDevKey
    {
      get
      {
        return Settings.GetSetting("YouTube.DeveloperKey");
      }
    }

    /// <summary>
    /// Returns max results count for a YouTube query.
    /// </summary>
    public int MaxResultsCount
    {
      get { return Settings.GetIntSetting("YouTube.MaxResults", 25); }
    }

    #endregion YouTube helpers
  }
}