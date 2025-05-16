using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AccountManager.DAL
{

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class ActionableMessage
    {
        public string resourceUrl { get; set; }
    }

    public class Ads
    {
        public string astJsURL { get; set; }
    }

    public class Analytics
    {
        public List<string> disabledDatapoints { get; set; }
    }

    public class AppHealth
    {
        public int appHealthIntervalInMs { get; set; }
    }

    public class ApplicationSettings
    {
        public string StaticMapUrl { get; set; }
        public string MapControlKey { get; set; }
        public int MaxRequestLength { get; set; }
        public string DownloadUrlBase { get; set; }
        public bool VDirIsPublicProperty { get; set; }
        public double DownloadTokenRefreshMinutes { get; set; }
        public string FirstDownloadToken { get; set; }
    }

    public class ApplicationSettings2
    {
        public string configIDs { get; set; }
        public Settings settings { get; set; }
    }

    public class AttachmentPolicy
    {
        public List<string> AllowedFileTypes { get; set; }
        public List<string> AllowedMimeTypes { get; set; }
        public List<string> ForceSaveFileTypes { get; set; }
        public List<string> ForceSaveMimeTypes { get; set; }
        public List<string> BlockedFileTypes { get; set; }
        public List<string> BlockedMimeTypes { get; set; }
        public string ActionForUnknownFileAndMIMETypes { get; set; }
        public List<string> WacViewableFileTypes { get; set; }
        public List<string> WacEditableFileTypes { get; set; }
        public List<string> WacConvertibleFileTypes { get; set; }
        public List<List<string>> WacPreloadMappings { get; set; }
        public bool WacViewingOnPublicComputersEnabled { get; set; }
        public bool WacViewingOnPrivateComputersEnabled { get; set; }
        public bool ForceWacViewingFirstOnPublicComputers { get; set; }
        public bool ForceWacViewingFirstOnPrivateComputers { get; set; }
        public bool PrintWithoutDownloadEnabled { get; set; }
        public bool DirectFileAccessOnPublicComputersEnabled { get; set; }
        public bool DirectFileAccessOnPrivateComputersEnabled { get; set; }
        public bool AttachmentDataProviderAvailable { get; set; }
        public bool DropboxAttachmentsEnabled { get; set; }
        public bool BoxAttachmentsEnabled { get; set; }
        public bool OnedriveAttachmentsEnabled { get; set; }
        public bool GoogleDriveAttachmentsEnabled { get; set; }
        public bool ClassicAttachmentsEnabled { get; set; }
        public bool ReferenceAttachmentsEnabled { get; set; }
        public bool SaveAttachmentsToCloudEnabled { get; set; }
        public bool ThirdPartyAttachmentsEnabled { get; set; }
        public bool GroupsOneDriveDataProviderAvailable { get; set; }
        public bool ConditionalAccessDirectFileAccessOnPrivateComputersBlocked { get; set; }
        public bool ConditionalAccessDirectFileAccessOnPublicComputersBlocked { get; set; }
        public bool ConditionalAccessPrintWithoutDownloadBlocked { get; set; }
        public bool ConditionalAccessWacViewingOnPrivateComputersBlocked { get; set; }
        public bool ConditionalAccessWacViewingOnPublicComputersBlocked { get; set; }
    }

    public class BingNativeAds
    {
        public string placementOther1 { get; set; }
        public string placementOther2 { get; set; }
        public string placementPrimary { get; set; }
        public string placementHeader { get; set; }
    }

    public class Body
    {
        public List<Conversation> Conversations { get; set; }
        public int TotalConversationsInView { get; set; }
        public int IndexedOffset { get; set; }
        public SearchFolderId SearchFolderId { get; set; }
        public FolderId FolderId { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseClass { get; set; }
        public string __type { get; set; }
        public ResponseMessages ResponseMessages { get; set; }
    }

    public class CLPAudit
    {
        public string resourceUrl { get; set; }
    }

    public class Conversation
    {
        public ConversationId ConversationId { get; set; }
        public string ConversationTopic { get; set; }
        public List<string> UniqueSenders { get; set; }
        public DateTime LastDeliveryTime { get; set; }
        public bool HasAttachments { get; set; }
        public int MessageCount { get; set; }
        public int GlobalMessageCount { get; set; }
        public int UnreadCount { get; set; }
        public int GlobalUnreadCount { get; set; }
        public int Size { get; set; }
        public List<string> ItemClasses { get; set; }
        public string Importance { get; set; }
        public List<ItemId> ItemIds { get; set; }
        public List<GlobalItemId> GlobalItemIds { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public string InstanceKey { get; set; }
        public string Preview { get; set; }
        public bool HasIrm { get; set; }
        public bool IsLocked { get; set; }
        public FamilyId FamilyId { get; set; }
        public DateTime LastDeliveryOrRenewTime { get; set; }
        public DateTime LastSentTime { get; set; }
        public bool HasAttachmentPreviews { get; set; }
        public LastSender LastSender { get; set; }
        public List<int> SystemCategories { get; set; }
    }

    public class ConversationId
    {
        public string __type { get; set; }
        public string Id { get; set; }
    }

    public class Copilot
    {
        public bool elaborateEnabled { get; set; }
        public bool coachEnabled { get; set; }
        public bool summarizeEnabled { get; set; }
        public bool suggestedDraftsEnabled { get; set; }
    }

    public class DefaultFolderId
    {
        public string __type { get; set; }
        public string Id { get; set; }
        public string ChangeKey { get; set; }
    }

    public class DefaultList
    {
        public string Name { get; set; }
        public int Color { get; set; }
        public DateTime LastTimeUsed { get; set; }
    }

    public class Diagnostics
    {
        public bool panel { get; set; }
    }

    public class DistributionGroups
    {
        public string exchangePortalUrl { get; set; }
    }

    public class EffectiveRights
    {
        public bool CreateAssociated { get; set; }
        public bool CreateContents { get; set; }
        public bool CreateHierarchy { get; set; }
        public bool Delete { get; set; }
        public bool Modify { get; set; }
        public bool Read { get; set; }
        public bool ViewPrivateItems { get; set; }
    }

    public class ExtendedFieldURI
    {
        public string __type { get; set; }
        public string PropertyTag { get; set; }
        public string PropertyType { get; set; }
    }

    public class ExtendedProperty
    {
        public ExtendedFieldURI ExtendedFieldURI { get; set; }
        public string Value { get; set; }
    }

    public class FamilyId
    {
        public string __type { get; set; }
        public string Id { get; set; }
    }

    public class Favorites
    {
        public List<Value> value { get; set; }
    }

    public class Features
    {
        [JsonProperty("ads-honor-globalPrivacyControl")]
        public bool adshonorglobalPrivacyControl { get; set; }

        [JsonProperty("cal-board-export-option")]
        public bool calboardexportoption { get; set; }

        [JsonProperty("cal-mf-calendarWeatherDetails")]
        public bool calmfcalendarWeatherDetails { get; set; }

        [JsonProperty("fwk-webPushNotification")]
        public bool fwkwebPushNotification { get; set; }

        [JsonProperty("fwk-mailtoProtocolHandler")]
        public bool fwkmailtoProtocolHandler { get; set; }

        [JsonProperty("cal-advancedRoomBooking")]
        public bool caladvancedRoomBooking { get; set; }

        [JsonProperty("mon-ribbon-teaching-callout")]
        public bool monribbonteachingcallout { get; set; }

        [JsonProperty("mon-show-addins")]
        public bool monshowaddins { get; set; }

        [JsonProperty("cmp-ribbon-callout")]
        public bool cmpribboncallout { get; set; }

        [JsonProperty("fwk-nativeServiceWorker")]
        public bool fwknativeServiceWorker { get; set; }

        [JsonProperty("mon-altForKeytips")]
        public bool monaltForKeytips { get; set; }

        [JsonProperty("auth-redirectOnSessionTimeout")]
        public bool authredirectOnSessionTimeout { get; set; }

        [JsonProperty("mon-friendlyKeyboarding")]
        public bool monfriendlyKeyboarding { get; set; }

        [JsonProperty("notif-componentTokenProvider")]
        public bool notifcomponentTokenProvider { get; set; }

        [JsonProperty("ads-hideDisplayAds")]
        public bool adshideDisplayAds { get; set; }

        [JsonProperty("auth-activityFeedScopePatch")]
        public bool authactivityFeedScopePatch { get; set; }

        [JsonProperty("auth-promptRebootNativeHost")]
        public bool authpromptRebootNativeHost { get; set; }

        [JsonProperty("cmp-nativeConversationOptions")]
        public bool cmpnativeConversationOptions { get; set; }

        [JsonProperty("doc-copyLinkUsingClipboardApi")]
        public bool doccopyLinkUsingClipboardApi { get; set; }

        [JsonProperty("doc-editInBrowserReuseSameWindow")]
        public bool doceditInBrowserReuseSameWindow { get; set; }

        [JsonProperty("doc-hideFileProviders")]
        public bool dochideFileProviders { get; set; }

        [JsonProperty("doc-smimeExtension")]
        public bool docsmimeExtension { get; set; }

        [JsonProperty("fwk-freezeDry")]
        public bool fwkfreezeDry { get; set; }

        [JsonProperty("fwk-recordPerformanceTrace")]
        public bool fwkrecordPerformanceTrace { get; set; }

        [JsonProperty("mail-openInNewWindow")]
        public bool mailopenInNewWindow { get; set; }

        [JsonProperty("mail-updateHostAppIcon")]
        public bool mailupdateHostAppIcon { get; set; }

        [JsonProperty("me-controlAccountSwitching")]
        public bool mecontrolAccountSwitching { get; set; }

        [JsonProperty("mon-errorMessage")]
        public bool monerrorMessage { get; set; }

        [JsonProperty("mon-projectionCustomTitlebar")]
        public bool monprojectionCustomTitlebar { get; set; }

        [JsonProperty("notif-multipleAccountSignalR")]
        public bool notifmultipleAccountSignalR { get; set; }

        [JsonProperty("rp-addBlankTargetToLinks")]
        public bool rpaddBlankTargetToLinks { get; set; }

        [JsonProperty("rp-messageBodyLocalPathHandler")]
        public bool rpmessageBodyLocalPathHandler { get; set; }

        [JsonProperty("rp-renderActionableMessage")]
        public bool rprenderActionableMessage { get; set; }

        [JsonProperty("sea-showOwaBranding")]
        public bool seashowOwaBranding { get; set; }

        [JsonProperty("tri-showArchiveUnreadCount")]
        public bool trishowArchiveUnreadCount { get; set; }

        [JsonProperty("usv-nps-outlookDialog")]
        public bool usvnpsoutlookDialog { get; set; }

        [JsonProperty("fwk-appLauncher")]
        public bool fwkappLauncher { get; set; }

        [JsonProperty("fwk-reboot")]
        public bool fwkreboot { get; set; }

        [JsonProperty("fwk-hideAppBar")]
        public bool fwkhideAppBar { get; set; }

        [JsonProperty("acct-connected-settings")]
        public bool acctconnectedsettings { get; set; }

        [JsonProperty("cal-roomFinderFreeBusyStyles")]
        public bool calroomFinderFreeBusyStyles { get; set; }

        [JsonProperty("auth-msalTokenFetch")]
        public bool authmsalTokenFetch { get; set; }

        [JsonProperty("cmp-postDraft-SavedMessageToHost")]
        public bool cmppostDraftSavedMessageToHost { get; set; }
    }

    public class FindConversation
    {
        public Header Header { get; set; }
        public Body Body { get; set; }
    }

    public class FindFolders
    {
        public Header Header { get; set; }
        public Body Body { get; set; }
    }

    public class Folder
    {
        public string __type { get; set; }
        public FolderId FolderId { get; set; }
        public int UnreadCount { get; set; }
        public ParentFolderId ParentFolderId { get; set; }
        public string FolderClass { get; set; }
        public string DisplayName { get; set; }
        public int TotalCount { get; set; }
        public int ChildFolderCount { get; set; }
        public List<ExtendedProperty> ExtendedProperty { get; set; }
        public EffectiveRights EffectiveRights { get; set; }
        public string DistinguishedFolderId { get; set; }
        public string Charm { get; set; }
    }

    public class FolderId
    {
        public string __type { get; set; }
        public string Id { get; set; }
        public string ChangeKey { get; set; }
    }

    public class FolderParams
    {
        public string TimeZoneStr { get; set; }
    }

    public class ForceReboot
    {
        public bool WebForceRebootEnabled { get; set; }
    }

    public class GdprAds
    {
        public string vendorListCdnUrl { get; set; }
    }

    public class GlobalItemId
    {
        public string __type { get; set; }
        public string ChangeKey { get; set; }
        public string Id { get; set; }
    }

    public class Graph
    {
        public string resourceUrl { get; set; }
    }

    public class GroupsSets
    {
        public List<object> UnifiedGroupsSets { get; set; }
    }

    public class Header
    {
        public ServerVersionInfo ServerVersionInfo { get; set; }
    }

    public class InboxShopping
    {
        public string allowedDomainsURL { get; set; }
    }

    public class Item
    {
        public string Id { get; set; }
        public object Value { get; set; }
        public string __type { get; set; }
        public RootFolder RootFolder { get; set; }
        public SearchFolderId SearchFolderId { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseClass { get; set; }
    }

    public class ItemId
    {
        public string __type { get; set; }
        public string ChangeKey { get; set; }
        public string Id { get; set; }
    }

    public class LastSender
    {
        public Mailbox Mailbox { get; set; }
    }

    public class Loki
    {
        public string resourceUrl { get; set; }
    }

    public class Mailbox
    {
        public string Name { get; set; }
        public string EmailAddress { get; set; }
        public string RoutingType { get; set; }
    }

    public class MailboxTimeZoneOffset
    {
        public string TimeZoneId { get; set; }
        public List<OffsetRange> OffsetRanges { get; set; }
        public string TimeZoneName { get; set; }
        public List<string> IanaTimeZones { get; set; }
    }

    public class MasterCategoryList
    {
        public List<MasterList> MasterList { get; set; }
        public List<DefaultList> DefaultList { get; set; }
    }

    public class MasterList
    {
        public string Name { get; set; }
        public int Color { get; set; }
        public string Id { get; set; }
        public DateTime LastTimeUsed { get; set; }
    }

    public class MessageParams
    {
        public string TimeZoneStr { get; set; }
        public int InboxReadingPanePosition { get; set; }
        public bool IsFocusedInboxOn { get; set; }
        public bool BootWithConversationView { get; set; }
        public List<SortResult> SortResults { get; set; }
    }

    public class MetaOSSettings
    {
        public string dataResidency { get; set; }
    }

    public class MSALUrl
    {
        public List<string> resourceBlockList { get; set; }
        public string outlookgatewayUrl { get; set; }
        public string notificationChannelUrl { get; set; }
    }

    public class MsPlacesDevRing
    {
        public bool enabled { get; set; }
    }

    public class MsPlacesWebEnabled
    {
        public bool enabled { get; set; }
    }

    public class NativeReactions
    {
        public string deviceType { get; set; }
    }

    public class OffsetRange
    {
        public DateTime UtcTime { get; set; }
        public int Offset { get; set; }
    }

    public class OneNote
    {
        public string ariaIngestionKeyOverride { get; set; }
    }

    public class OutlookServiceUrl
    {
        public string outlookServiceDomain { get; set; }
    }

    public class FindMailFolderItem
    {
        public Header Header { get; set; }
        public Body Body { get; set; }
    }

    public class OwaUserConfig
    {
        public UserOptions UserOptions { get; set; }
        public SessionSettings SessionSettings { get; set; }
        public SegmentationSettings SegmentationSettings { get; set; }
        public AttachmentPolicy AttachmentPolicy { get; set; }
        public PolicySettings PolicySettings { get; set; }
        public ApplicationSettings ApplicationSettings { get; set; }
        public ViewStateConfiguration ViewStateConfiguration { get; set; }
        public MasterCategoryList MasterCategoryList { get; set; }
        public SmimeAdminSettings SmimeAdminSettings { get; set; }
        public SafetyUserOptions SafetyUserOptions { get; set; }
        public GroupsSets GroupsSets { get; set; }
        public int MailTipsLargeAudienceThreshold { get; set; }
        public List<object> RetentionPolicyTags { get; set; }
        public int MaxRecipientsPerMessage { get; set; }
        public bool RecoverDeletedItemsEnabled { get; set; }
        public bool GroupsEnabled { get; set; }
        public bool PublicComputersDetectionEnabled { get; set; }
        public bool PolicyTipsEnabled { get; set; }
        public string HexCID { get; set; }
        public bool LinkPreviewEnabled { get; set; }
        public bool IsConsumerChild { get; set; }
        public bool AsyncSendEnabled { get; set; }
        public string AdMarket { get; set; }
        public string ConsumerUserCountry { get; set; }
        public bool RmsV2UIEnabled { get; set; }
        public Favorites Favorites { get; set; }
        public PrimeSettings PrimeSettings { get; set; }
        public TenantThemeData TenantThemeData { get; set; }
        public DateTime MailboxCreateDate { get; set; }
        public bool IsMonitoring { get; set; }
    }

    public class ParentFolder
    {
        public string __type { get; set; }
        public FolderId FolderId { get; set; }
        public int UnreadCount { get; set; }
        public ParentFolderId ParentFolderId { get; set; }
        public string DisplayName { get; set; }
        public int TotalCount { get; set; }
        public int ChildFolderCount { get; set; }
        public List<ExtendedProperty> ExtendedProperty { get; set; }
        public EffectiveRights EffectiveRights { get; set; }
        public string DistinguishedFolderId { get; set; }
    }

    public class ParentFolderId
    {
        public string __type { get; set; }
        public string Id { get; set; }
        public string ChangeKey { get; set; }
    }

    public class Path
    {
        public string __type { get; set; }
        public string FieldURI { get; set; }
    }

    public class PlacesPrivateDev
    {
        public bool enabled { get; set; }
    }

    public class PlacesPrivateDevRing
    {
        public bool enabled { get; set; }
    }

    public class PoisonedBuild
    {
        public List<object> skipBuilds { get; set; }
    }

    public class PolicySettings
    {
        public string OutboundCharset { get; set; }
        public bool UseGB18030 { get; set; }
        public bool UseISO885915 { get; set; }
        public string InstantMessagingType { get; set; }
        public bool PlacesEnabled { get; set; }
        public bool WeatherEnabled { get; set; }
        public bool LocalEventsEnabled { get; set; }
        public bool InterestingCalendarsEnabled { get; set; }
        public bool OutlookBetaToggleEnabled { get; set; }
        public bool ExternalImageProxyEnabled { get; set; }
        public bool MessagePreviewsDisabled { get; set; }
        public bool IsSharedActivityBasedTimeoutEnabled { get; set; }
        public bool NpsSurveysEnabled { get; set; }
        public bool ShowOnlineArchiveEnabled { get; set; }
        public bool PersonalAccountCalendarsEnabled { get; set; }
        public bool TeamsnapCalendarsEnabled { get; set; }
        public bool BookingsMailboxCreationEnabled { get; set; }
        public bool FeedbackEnabled { get; set; }
        public bool SMimeSuppressNameChecksEnabled { get; set; }
        public bool BizBarEnabled { get; set; }
        public bool EmptyStateEnabled { get; set; }
        public bool OfflineEnabledWeb { get; set; }
        public bool OfflineEnabledWin { get; set; }
        public bool OneWinNativeOutlookEnabled { get; set; }
        public bool PersonalAccountsEnabled { get; set; }
        public bool AdditionalAccountsEnabled { get; set; }
        public bool ChangeSettingsAccountEnabled { get; set; }
        public bool ItemsToOtherAccountsEnabled { get; set; }
    }

    public class PrimeSettings
    {
        public List<Item> Items { get; set; }
    }

    public class ReactOptinSettings
    {
        public bool CalendarMobileEnabled { get; set; }
        public bool PeopleMobileEnabled { get; set; }
        public bool TasksEnabled { get; set; }
        public bool TasksGraduatedFromBeta { get; set; }
        public string TasksRedirectUrl { get; set; }
        public bool MiniGraduatedFromBeta { get; set; }
    }

    public class ReliabilityCheck
    {
        public bool isPresent { get; set; }
    }

    public class ResponseMessages
    {
        public List<Item> Items { get; set; }
    }

    public class OutlookStartupdata
    {
        public OwaUserConfig owaUserConfig { get; set; }
        public List<string> features { get; set; }
        public ApplicationSettings applicationSettings { get; set; }
        public long currentEpochInMs { get; set; }
        public List<object> skipBuilds { get; set; }
        public string extraSettings { get; set; }
        public string language { get; set; }
        public FindConversation findConversation { get; set; }
        public MessageParams messageParams { get; set; }
        public FindFolders findFolders { get; set; }
        public FindMailFolderItem findMailFolderItem { get; set; }
        public FolderParams folderParams { get; set; }
    }

    public class RootFolder
    {
        public string __type { get; set; }
        public List<Folder> Folders { get; set; }
        public ParentFolder ParentFolder { get; set; }
        public bool CustomSorted { get; set; }
        public int IndexedPagingOffset { get; set; }
        public int TotalItemsInView { get; set; }
        public bool IncludesLastItemInRange { get; set; }
    }

    public class Safelinks
    {
        public bool isSupported { get; set; }
    }

    public class SafetyUserOptions
    {
        public bool ReportToExternalSender { get; set; }
        public bool BlockContentFromUnknownSenders { get; set; }
    }

    public class SearchFolderId
    {
        public string __type { get; set; }
        public string Id { get; set; }
        public string ChangeKey { get; set; }
    }

    public class SegmentationSettings
    {
        public bool ReportJunkEmailEnabled { get; set; }
        public bool StickyNotes { get; set; }
        public bool Rules { get; set; }
        public bool Themes { get; set; }
        public bool JunkEMail { get; set; }
        public bool InstantMessage { get; set; }
        public bool Irm { get; set; }
        public bool DisplayPhotos { get; set; }
        public bool SetPhoto { get; set; }
        public bool OnSendAddinsEnabled { get; set; }
    }

    public class ServerVersionInfo
    {
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public int MajorBuildNumber { get; set; }
        public int MinorBuildNumber { get; set; }
        public string Version { get; set; }
    }

    public class SessionSettings
    {
        public bool IsExplicitLogon { get; set; }
        public bool IsPublicLogon { get; set; }
        public bool IsPublicComputerSession { get; set; }
        public bool IsBposUser { get; set; }
        public bool IsPremiumConsumerMailbox { get; set; }
        public bool IsProsumerConsumerMailbox { get; set; }
        public string BirthdayPrecision { get; set; }
        public string UserDisplayName { get; set; }
        public string UserPrincipalName { get; set; }
        public string UserEmailAddress { get; set; }
        public string UserLegacyExchangeDN { get; set; }
        public List<string> UserProxyAddresses { get; set; }
        public bool IsShadowMailbox { get; set; }
        public List<object> DisposableEmailAddresses { get; set; }
        public string DefaultFromAddress { get; set; }
        public string LogonEmailAddress { get; set; }
        public string MailboxGuid { get; set; }
        public bool IsDumpsterOverQuota { get; set; }
        public bool HasArchive { get; set; }
        public string ArchiveDisplayName { get; set; }
        public string ArchiveMailboxGuid { get; set; }
        public int MaxMessageSizeInKb { get; set; }
        public List<DefaultFolderId> DefaultFolderIds { get; set; }
        public List<string> DefaultFolderNames { get; set; }
        public string UserCulture { get; set; }
        public string UserLanguage { get; set; }
        public string UserPuid { get; set; }
        public string EncryptedUserPuid { get; set; }
        public string TenantDomain { get; set; }
        public string OrganizationDomain { get; set; }
        public string TenantGuid { get; set; }
        public string ExternalDirectoryUserGuid { get; set; }
        public string ExternalDirectoryTenantGuid { get; set; }
        public bool IsUserCultureRightToLeft { get; set; }
        public string HelpUrl { get; set; }
        public bool IsSPOnPrem { get; set; }
        public bool IsSubstrateSearchServiceAvailable { get; set; }
        public int PremiumAccountOffers { get; set; }
        public int WebSessionType { get; set; }
    }

    public class Settings
    {
        public Features Features { get; set; }
        public DistributionGroups DistributionGroups { get; set; }
        public MsPlacesWebEnabled MsPlacesWebEnabled { get; set; }
        public MsPlacesDevRing MsPlacesDevRing { get; set; }
        public PlacesPrivateDev PlacesPrivateDev { get; set; }
        public OutlookServiceUrl OutlookServiceUrl { get; set; }
        public ToDo ToDo { get; set; }
        public Analytics Analytics { get; set; }
        public ActionableMessage ActionableMessage { get; set; }
        public GdprAds GdprAds { get; set; }
        public Copilot Copilot { get; set; }
        public UniversalMeControl UniversalMeControl { get; set; }
        public PlacesPrivateDevRing PlacesPrivateDevRing { get; set; }
        public PoisonedBuild PoisonedBuild { get; set; }
        public Diagnostics Diagnostics { get; set; }
        public Graph Graph { get; set; }
        public NativeReactions NativeReactions { get; set; }
        public SuggestedAttachmentsRecommendationServiceCall SuggestedAttachmentsRecommendationServiceCall { get; set; }
        public ReliabilityCheck ReliabilityCheck { get; set; }
        public CLPAudit CLPAudit { get; set; }
        public MetaOSSettings MetaOSSettings { get; set; }
        public Safelinks Safelinks { get; set; }
        public MSALUrl MSALUrl { get; set; }
        public XandrNativeAds XandrNativeAds { get; set; }
        public BingNativeAds BingNativeAds { get; set; }
        public TaboolaNativeAds TaboolaNativeAds { get; set; }
        public TripleliftNativeAds TripleliftNativeAds { get; set; }
        public InboxShopping InboxShopping { get; set; }
        public Ads Ads { get; set; }
        public TeamsURL TeamsURL { get; set; }
        public AppHealth AppHealth { get; set; }
        public OneNote OneNote { get; set; }
        public ForceReboot ForceReboot { get; set; }
        public Loki Loki { get; set; }
    }

    public class SingleValueSetting
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class SmimeAdminSettings
    {
        public bool AlwaysSign { get; set; }
        public bool AlwaysEncrypt { get; set; }
        public bool ClearSign { get; set; }
        public bool IncludeCertificateChainWithoutRootCertificate { get; set; }
        public bool IncludeCertificateChainAndRootCertificate { get; set; }
        public bool EncryptTemporaryBuffers { get; set; }
        public bool SignedEmailCertificateInclusion { get; set; }
        public int BccEncryptedEmailForking { get; set; }
        public bool IncludeSMIMECapabilitiesInMessage { get; set; }
        public bool CopyRecipientHeaders { get; set; }
        public bool OnlyUseSmartCard { get; set; }
        public bool TripleWrapSignedEncryptedMail { get; set; }
        public string EncryptionAlgorithms { get; set; }
        public string SigningAlgorithms { get; set; }
        public bool AllowUserChoiceOfSigningCertificate { get; set; }
    }

    public class SortResult
    {
        public Path Path { get; set; }
        public string Order { get; set; }
    }

    public class SuggestedAttachmentsRecommendationServiceCall
    {
        public string ScenarioName { get; set; }
    }

    public class TaboolaNativeAds
    {
        public string apiKey { get; set; }
        public string publishIdFormat { get; set; }
        public string placementOther1 { get; set; }
        public string placementOther2 { get; set; }
        public string placementPrimary { get; set; }
        public string placementHeader { get; set; }
        public string placementNonInbox { get; set; }
        public string placementFloatingInbox { get; set; }
        public string placementFloatingOther { get; set; }
    }

    public class TeamsURL
    {
        public string resourceURL { get; set; }
    }

    public class TenantThemeData
    {
        public bool UserPersonalizationAllowed { get; set; }
    }

    public class ToDo
    {
        public bool isToDoFeaturesEnabled { get; set; }
    }

    public class TripleliftNativeAds
    {
        public string placementOther1 { get; set; }
        public string placementOther2 { get; set; }
        public string placementPrimary { get; set; }
        public string placementHeader { get; set; }
        public string placementNonInbox { get; set; }
        public string placementFloatingInbox { get; set; }
        public string placementFloatingOther { get; set; }
    }

    public class UniversalMeControl
    {
        public bool enabled { get; set; }
    }

    public class UserOptions
    {
        public string TimeZone { get; set; }
        public string TimeFormat { get; set; }
        public string DateFormat { get; set; }
        public int WeekStartDay { get; set; }
        public int HourIncrement { get; set; }
        public bool ShowWeekNumbers { get; set; }
        public int FirstWeekOfYear { get; set; }
        public int WeatherEnabled { get; set; }
        public int WeatherUnit { get; set; }
        public List<object> WeatherLocations { get; set; }
        public int LocalEventsEnabled { get; set; }
        public bool EnableReminders { get; set; }
        public bool EnableReminderSound { get; set; }
        public bool EnableReactions { get; set; }
        public bool EnableReactionSound { get; set; }
        public int DefaultOnlineMeetingProvider { get; set; }
        public string AllowedOnlineMeetingProviders { get; set; }
        public int NewItemNotify { get; set; }
        public bool ShowNotificationBar { get; set; }
        public bool SmimeEncrypt { get; set; }
        public bool SmimeSign { get; set; }
        public bool AlwaysShowBcc { get; set; }
        public bool AlwaysShowFrom { get; set; }
        public string ComposeMarkup { get; set; }
        public int ComposeFontSize { get; set; }
        public string ComposeFontColor { get; set; }
        public int ComposeFontFlags { get; set; }
        public bool AutoAddSignature { get; set; }
        public bool AutoAddSignatureOnReply { get; set; }
        public bool AutoAddSignatureOnMobile { get; set; }
        public bool UseDesktopSignature { get; set; }
        public string PreviewMarkAsRead { get; set; }
        public string DisplayDensityMode { get; set; }
        public string ConsumerAdsExperimentMode { get; set; }
        public bool CheckForForgottenAttachments { get; set; }
        public int MarkAsReadDelaytime { get; set; }
        public string NextSelection { get; set; }
        public string ReadReceipt { get; set; }
        public bool EmptyDeletedItemsOnLogoff { get; set; }
        public int NavigationBarWidth { get; set; }
        public bool IsFavoritesFolderTreeCollapsed { get; set; }
        public bool IsGroupsTreeCollapsed { get; set; }
        public bool ShowReadingPaneOnFirstLoad { get; set; }
        public bool IsMailRootFolderTreeCollapsed { get; set; }
        public string ThemeStorageId { get; set; }
        public bool IsDarkModeTheme { get; set; }
        public int MiniDarkMode { get; set; }
        public long NewEnabledPonts { get; set; }
        public string FlagAction { get; set; }
        public bool ManuallyPickCertificate { get; set; }
        public bool ShowInlinePreviews { get; set; }
        public string ConversationSortOrder { get; set; }
        public string ConversationSortOrderReact { get; set; }
        public bool HideDeletedItems { get; set; }
        public WorkingHours WorkingHours { get; set; }
        public int DefaultReminderTimeInMinutes { get; set; }
        public List<MailboxTimeZoneOffset> MailboxTimeZoneOffset { get; set; }
        public int KeyboardShortcutsMode { get; set; }
        public bool EchoGroupMessageBackToSubscribedSender { get; set; }
        public bool ShowSenderOnTopInListView { get; set; }
        public bool ShowPreviewTextInListView { get; set; }
        public int GlobalReadingPanePosition { get; set; }
        public int GlobalReadingPanePositionReact { get; set; }
        public int GlobalListViewTypeReact { get; set; }
        public bool ReportJunkSelected { get; set; }
        public bool CheckForReportJunkDialog { get; set; }
        public List<string> FrequentlyUsedFolders { get; set; }
        public int FavoritesBitFlags { get; set; }
        public string ArchiveFolderId { get; set; }
        public int MaxPersonasToDelete { get; set; }
        public int MaximumNumberOfContactsPerPerson { get; set; }
        public bool UseBoldCalendarColorThemeReact { get; set; }
        public bool IsReplyAllTheDefaultResponse { get; set; }
        public bool LinkPreviewEnabled { get; set; }
        public int MailSendUndoInterval { get; set; }
        public int DefaultMeetingDuration { get; set; }
        public bool AddOnlineMeetingToAllEvents { get; set; }
        public bool IsFocusedInboxEnabled { get; set; }
        public string IsFocusedInboxOnLastUpdateTime { get; set; }
        public string IsFocusedInboxOnAdminLastUpdateTime { get; set; }
        public bool IsFocusedInboxCapable { get; set; }
        public bool FocusedInboxServerOverride { get; set; }
        public string FlightScopeOverride { get; set; }
        public string PlusOverrides { get; set; }
        public bool PreferAccessibleContent { get; set; }
        public int ClientTypeOptInState { get; set; }
        public int TasksClientTypeOptInState { get; set; }
        public ReactOptinSettings ReactOptinSettings { get; set; }
        public bool WebSuggestedRepliesEnabledForUser { get; set; }
        public string FirstOWALogon { get; set; }
        public string FirstOWAReactMailLogon { get; set; }
        public string FirstOWAReactMiniLogon { get; set; }
        public bool MobileAppEducationEnabled { get; set; }
        public bool OutlookGifPickerDisabled { get; set; }
        public bool WorkspaceUserEnabled { get; set; }
        public bool OutlookTextPredictionDisabled { get; set; }
        public bool SendFromAliasEnabled { get; set; }
        public bool MessageRemindersEnabled { get; set; }
        public bool MessageHighlightsEnabled { get; set; }
        public bool IsTopicHighlightsEnabled { get; set; }
        public bool HasYammerLicense { get; set; }
    }

    public class Value
    {
        public string Client { get; set; }
        public string Id { get; set; }
        public int Index { get; set; }
        public string DisplayName { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public List<SingleValueSetting> SingleValueSettings { get; set; }
        public string Type { get; set; }
    }

    public class ViewStateConfiguration
    {
        public int TemperatureUnit { get; set; }
        public int CalendarViewTypeDesktop { get; set; }
        public bool CalendarViewInSplitMode { get; set; }
        public bool CalendarViewShowDeclinedMeetings { get; set; }
        public bool CalendarSidePanelIsExpanded { get; set; }
        public int CalendarSidePanelWidth { get; set; }
        public List<string> SelectedCalendarsDesktop { get; set; }
        public int PeopleHubDisplayOption { get; set; }
        public int PeopleHubSortOption { get; set; }
        public int AttachmentsFilePickerViewTypeForMouse { get; set; }
        public int AttachOrShareCloudFiles { get; set; }
        public int AttachOrShareGroupsFiles { get; set; }
        public int FocusedInboxBitFlags { get; set; }
        public int SearchBitFlags { get; set; }
        public int ListViewBitFlags { get; set; }
        public int MailLeftSwipeAction { get; set; }
        public int MailRightSwipeAction { get; set; }
        public List<int> MailTriageOnHoverActions { get; set; }
        public string MailRibbonConfig { get; set; }
        public string MailTriageActionConfig { get; set; }
        public string ClutterViewWatermark { get; set; }
        public string FocusedViewWatermark { get; set; }
        public int BookingCalendarViewType { get; set; }
        public int BookingsTilesVisited { get; set; }
        public int BookingsTilesDismissed { get; set; }
        public bool Bookingsv2IsOptedIn { get; set; }
        public bool IsOnboardedInBookingsv2 { get; set; }
        public int BookingsV2OptInCount { get; set; }
        public int AccessibilityBitFlags { get; set; }
        public bool BookingsV2IsOnboardedFREModal { get; set; }
        public bool BookingsIsNpsFormSubmitted { get; set; }
        public string BookingsNpsPopupDismissTimeStamp { get; set; }
        public string BookingsFirstSeenTimeStamp { get; set; }
        public int BookingsLastWhatsNewCalloutShown { get; set; }
        public int BookingsLastWhatsNewCardShown { get; set; }
        public bool BookingsV2IsOnboardedFRECoachmark { get; set; }
        public bool BookingsV2IsOnboardedFiltersCoachmark { get; set; }
        public bool BookingsV2IsOnboardedApproveCalendarCoachmark { get; set; }
        public bool IsBookingsV2DefaultOnUser { get; set; }
    }

    public class WorkingHours
    {
        public int WorkHoursStartTimeInMinutes { get; set; }
        public int WorkHoursEndTimeInMinutes { get; set; }
        public int WorkDays { get; set; }
        public string WorkingHoursTimeZoneId { get; set; }
    }

    public class XandrNativeAds
    {
        public string placementOther1 { get; set; }
        public string placementOther2 { get; set; }
        public string placementPrimary { get; set; }
        public string placementHeader { get; set; }
        public string placementNonInbox { get; set; }
        public string placementFloatingInbox { get; set; }
        public string placementFloatingOther { get; set; }
    }


}
