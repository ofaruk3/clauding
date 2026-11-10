using Microsoft.Web.WebView2.Core;

namespace Clauding;

/// <summary>One WebView2 environment for every page (scene, panel): two environments on the same
/// user-data folder with different options are refused by WebView2.</summary>
static class WebOrtam
{
    static Task<CoreWebView2Environment>? ortam;

    public static Task<CoreWebView2Environment> Al() => ortam ??= CoreWebView2Environment.CreateAsync(null,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClaudingData", "WebView2"),
        new CoreWebView2EnvironmentOptions("--disable-features=msSmartScreenProtection"));
}
