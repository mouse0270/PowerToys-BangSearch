using ManagedCommon;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Plugin;

namespace BangSearch {
    public class Bang {
        public string Trigger { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
        public List<string> Urls { get; set; } = new List<string>();
    }

    public static class StringExtensions {
        public static string ToTitleCase(this string str) {
            if (string.IsNullOrEmpty(str)) return str;
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }
    }

    public class Main : IPlugin, ISettingProvider {
        #region Core Properties
            // The ID of the plugin - this needs to be unique and match the ID in the plugin's manifest
            public static string PluginID => "39CACA36FD274314BEA058A0FACB97EC";
            // The name of the plugin
            public string Name => "BangSearch";
            // The description of the plugin
            public string Description => "A PowerToys Run plugin to use DuckDuckGo-like bangs directly from the PowerToys Run Bar.";
            // The Icon Path of the plugin
            private string IconPath { get; set; } = "Images/bangsearch.light.png";
            // The Context of the plugin
            private PluginInitContext Context { get; set; }

            private string FavIconUrl = "https://t0.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&size=32&url=http://%s";
            private bool DownloadIcons = true;
            /// <summary>
            /// Additional options for the plugin.
            /// </summary>
            public IEnumerable<PluginAdditionalOption> AdditionalOptions => [
                new() {
                    Key = nameof(FavIconUrl),
                    DisplayLabel = "FavIcon URL",
                    DisplayDescription = "URL used to fetch the favicon for the bangs.",
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                    TextValue  = FavIconUrl,
                },
                new () {
                    Key = nameof(DownloadIcons),
                    DisplayLabel = "Download Icons",
                    DisplayDescription = "Download icons for the bangs. If disabled, the default icon will be used.",
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                    Value = DownloadIcons,
                }
            ];
        #endregion Core Properties

        // Global Variable for All Initialized Bangs
        private List<Bang> Bangs { get; set; }

        // <summary>
        //   Loads a list of Bang objects from a specified JSON file.
        //   If the file does not exist, returns an empty list.
        // </summary>
        // <param name="filename">The name of the JSON file to load.</param>
        // <returns>A list of Bang objects.</returns>
        private List<Bang> GetBangs(string filename) {
            // Get the directory of the executing assembly
            string assemblyDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // Combine the directory with the filename to get the full path
            string filePath = System.IO.Path.Combine(assemblyDirectory, filename);

            // Check if the file exists
            // If the file does not exist, return an empty list
            if (!System.IO.File.Exists(filePath))  return new List<Bang>();

            try {
                // Read the content of the file
                // Deserialize the JSON content to a list of Bang objects
                return System.Text.Json.JsonSerializer.Deserialize<List<Bang>>(System.IO.File.ReadAllText(filePath));
            } catch (Exception e) { 
                // If an error occurs during reading or deserialization, return an empty list
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Bang>();
            }
        }

        /// <summary>
        ///   Loads and combines predefined and custom bangs from JSON files.
        /// </summary>
        private void LoadBangs() {
            // Load predefined bangs from "Bangs.json"
            // - This is the default list of bangs that comes with the plugin or is generated by the user
            // - These bangs are not editable by the user
            List<Bang> DDGBangs = GetBangs("Bangs.json");

            // Load custom bangs from "Custom.json"
            // - This is a list of bangs that the user has added
            // - These bangs are editable by the user
            List<Bang> CustomBangs = GetBangs("Custom.json");

            // Concatenate the predefined and custom bangs
            // - This creates a single list of all bangs
            Bangs = DDGBangs.Concat(CustomBangs).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns>A list of Results that are displayed in PowerToys Run</returns>
        public List<Result> Query(Query query) {
            // Split the search query into a bang and a search term
            // - The bang is the first word in the search query
            // - The search term is the rest of the search query
            string bang = query.Search.Contains(" ") ? query.Search.Substring(0, query.Search.IndexOf(" ")) : query.Search;
            string search = query.Search.Contains(" ") ? query.Search.Substring(query.Search.IndexOf(" ") + 1) : string.Empty;

            // Create an empty list of results
            List<Result> results = new List<Result>();

            // If the bang is "updatebangs", return a result to update the list of bangs
            if (bang.ToLower() == "fetch") return FetchBangs(search);

            // If the bang is "reloadbangs", return a result to reload the list of bangs
            if (bang.ToLower() == "reload") return ReloadBangs(search);

            // if the bang is "add", return a result to add a new bang
            if (bang.ToLower() == "add") return AddBang(search);

            // if the bang is "remove", return a result to remove an existing bang
            if (bang.ToLower() == "remove") return RemoveBang(search);
 
            // TODO: Implement a method to display the list of any matching bangs full or partial
            if (!string.IsNullOrEmpty(bang) && string.IsNullOrEmpty(search)) return QueryBangs(bang);

            // If the search term is empty, return an empty list of results
            if (string.IsNullOrEmpty(search)) return results;

            // Find all bangs that match the specified bang
            return QueryBangs(bang, search);
        }

        /// <summary>
        ///  Queries and displays a list of all available bangs that start with the specified trigger.
        ///  When the user selects a bang, the search box is cleared and the selected bang is filled in allowing the user to enter a search term.
        /// </summary>
        /// <param name="trigger">The trigger for the bang (e.g., "!g").</param>
        /// <returns>A list of Results that are displayed in PowerToys Run.</returns>
        public List<Result> QueryBangs(string trigger) {
            // Create an empty list of results
            List<Result> results = new List<Result>();

            // Find all bangs that match the specified trigger
            List<Bang> matchingBangs = Bangs.FindAll(b => b.Trigger.ToLower().StartsWith(trigger.ToLower()));

            // If no matching bangs are found, return an empty list of results
            if (matchingBangs.Count == 0) return results;

            // Create a result for each matching bang
            foreach (Bang bang in matchingBangs) {
                // Get the domain from the URL
                string domain = new Uri(bang.Urls[0]).Host.Replace("www.", "");

                // Create a result object
                Result result = new Result {
                    // Set the icon path for the result
                    IcoPath = GetBangImage(bang.Urls[0]),
                    // Set the title for the result
                        Title = bang.Title,
                        // Set the subtitle for the result
                        SubTitle = bang.SubTitle,
                        // Set the action for the result
                        Action = e => {

                            // Clear the search box
                            // - This is useful since the bang has already been executed
                            Context.API.ChangeQuery($"!{bang.Trigger} ", true);

                            // Return true to indicate that the action was successful
                            return false;
                        }
                    };
                    // Add the result to the list of results
                    results.Add(result);
            }

            // Return the list of results
            return results;
        }

        /// <summary>
        ///   Queries the list of bangs for any that match the specified trigger and search term.
        ///   If a matching bang is found, a result is created for each URL associated with the bang.
        /// </summary>
        /// <param name="trigger">The trigger for the bang (e.g., "!g").</param>
        /// <param name="search">The search term to use with the bang.</param>
        /// <returns>A list of Results that are displayed in PowerToys Run.</returns>
        public List<Result> QueryBangs(string trigger, string search) {
            // Create an empty list of results
            List<Result> results = new List<Result>();

            // Find all bangs that match the specified trigger
            List<Bang> matchingBangs = Bangs.FindAll(b => b.Trigger.ToLower() == trigger.ToLower());

            // If no matching bangs are found, return an empty list of results
            if (matchingBangs.Count == 0) return results;

            // Create a result for each matching bang
            foreach (Bang matching in matchingBangs) {
                foreach (string url in matching.Urls) {
                    // Get the domain from the URL
                    string domain = new Uri(url).Host.Replace("www.", "");

                    // Create a result object
                    Result result = new Result {
                        // Set the icon path for the result
                        IcoPath = GetBangImage(url),
                        // Set the title for the result
                        Title = $"{matching.Title} | {search}",
                        // Set the subtitle for the result
                        SubTitle = $"{matching.SubTitle}{(matching.SubTitle.Length > 0 ? " | " : " ")} URL {url.Replace("%s", search)}",
                        // Set the action for the result
                        Action = e => {
                            // Try to start a new process with the specified URL
                            try {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                                    FileName = url.Replace("%s", search),
                                    UseShellExecute = true
                                });
                            } catch (Exception ex) {
                                // If an error occurs, show an error message
                                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }

                            // Clear the search box
                            // - This is useful since the bang has already been executed
                            Context.API.ChangeQuery("", true);

                            // Return true to indicate that the action was successful
                            return true;
                        }
                    };
                    // Add the result to the list of results
                    results.Add(result);
                }
            }

            // Return the list of results
            return results;
        }

        /// <summary>
        ///   Creates a result to add a new bang to the list of custom bangs.
        ///   The bang is added with the specified trigger and URL.
        ///   The UI then prompts the user to enter a Title and SubTitle for the new bang.
        ///   The new bang is saved to the "Custom.json" file in the plugin's assembly directory.
        /// </summary>
        /// <param name="trigger">The trigger for the new bang.</param>
        /// <param name="title">The Title for the new bang.</param>
        /// <param name="subtitle">The SubTitle for the new bang.</param>
        /// <param name="url">The URL for the new bang.</param>
        /// <returns>A list of Results that are displayed in PowerToys Run.</returns>
        public List<Result> AddBang(string search) {
            // Split the search query into trigger, title?, subtitle?, and URL
            string trigger = string.Empty, title = string.Empty, subtitle = string.Empty, url = string.Empty;
            string pattern = @"^(?<trigger>\S+)(?:\s+(?<title>[^\|]+?))?(?:\s+\|\s+(?<subtitle>[^\s]+))?\s+(?<url>https?://\S+)$";
            var match = System.Text.RegularExpressions.Regex.Match(search, pattern);
            if (match.Success) {
                trigger = match.Groups["trigger"].Value.Trim();
                title = match.Groups["title"].Value.Trim();
                subtitle = match.Groups["subtitle"].Value.Trim();
                url = match.Groups["url"].Value.Trim();
            }

            // If the trigger is empty, return an empty list of results
            if (string.IsNullOrEmpty(trigger)) return new List<Result>();

            // If the URL is empty, return an empty list of results
            if (string.IsNullOrEmpty(url)) return new List<Result>();

            // Find all bangs that match the specified trigger
            return AddBang(trigger, title, subtitle, url);
        }
        public List<Result> AddBang(string trigger, string title, string subtitle, string url) {
            // Create an empty list of results
            List<Result> results = new List<Result>();

            // Create a new bang object with the specified trigger and URL
            Bang bang = new Bang {
                Trigger = trigger,
                Title = title,
                SubTitle = subtitle,
                Urls = new List<string> { url }
            };

            // Check if the new bang already exists in the list of custom bangs
            //if (Bangs.Exists(b => b.Trigger.ToLower() == trigger.ToLower())) return results;

            // check if the url has a search term
            if (!url.Contains("%s")) return results;

            // if the title is empty, set it to the domain. 
            // - for example google.com should become Google
            // - or images.google.com should become Google Images
            if (string.IsNullOrEmpty(title)) {
                string domain = new Uri(url).Host.Replace("www.", "");
                title = domain.Split('.')[0].ToTitleCase();
                if (domain.Split('.').Length > 2) title = $"{domain.Split('.')[0].ToTitleCase()} {domain.Split('.')[1].ToTitleCase()}";

                // update the Title
                bang.Title = title;
            }

            // Build SubTitle
            string subTitle = $"Add a new bang ({trigger}) with URL {url}";

            // If the title is not empty, add it to the SubTitle
            if (!string.IsNullOrEmpty(title)) subTitle = $"Add a new bang !{trigger} | {title} with URL {url}";
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(subtitle)) subTitle = $"Add a new bang !{trigger} | {title} - {subtitle} with URL {url}";

            // Add a result to prompt the user to enter a Title and SubTitle for the new bang
            results.Add(new Result {
                IcoPath = GetBangImage(url),
                Title = "Add Bang",
                SubTitle = $"Add a new bang ({trigger}) with URL {url}",
                Action = e => {
                    // Save the new bang to the list of custom bangs
                    SaveBang(bang);

                    // Clear the search box
                    // - This is useful since the bang has already been executed
                    Context.API.ChangeQuery($"!{bang.Trigger}", true);

                    // Return true to indicate that the action was successful
                    return true;
                }
            });

            // Return the list of results
            return results;
        }

        /// <summary>
        ///   Creates a result to remove an existing bang to the list of bangs.
        /// </summary>
        /// <param name="trigger">The trigger for the new bang.</param>
        /// <param name="url">The URL for the new bang.</param>
        /// <returns>A list of Results that are displayed in PowerToys Run.</returns>
        public List<Result> RemoveBang(string search) {
            // Split the search query into trigger and search term
            string trigger = search.Contains(" ") ? search.Substring(0, search.IndexOf(" ")) : search;

            // If the trigger is empty, return an empty list of results
            if (string.IsNullOrEmpty(trigger)) return new List<Result>();

            // Create a List of Bangs to remove
            List<Result> results = new List<Result>();

            // Find all bangs that match the specified trigger
            foreach (Bang bang in Bangs.FindAll(b => b.Trigger.ToLower() == trigger.ToLower())) {
                foreach (string url in bang.Urls) {
                    // Create a result to remove the bang
                    results.Add(new Result {
                        IcoPath = GetBangImage(url),
                        Title = $"Remove Bang",
                        SubTitle = $"Remove the bang !{trigger} with URL {url}",
                        Action = e => {
                            // Remove the bang from the list of bangs
                            RemoveBang(trigger, url);

                            // Update the list of Bangs
                            LoadBangs();

                            // Clear the search box
                            // - This is useful since the bang has already
                            Context.API.ChangeQuery("", true);

                            // Return true to indicate that the action was successful
                            return true;
                        }
                    });
                }
            }

            return results;
        }
        
        /// <summary>
        ///   Creates a result to update the list of bangs from DuckDuckGo's external-content service.
        /// </summary>
        /// <param name="search">The search term to use with the bang.</param>
        /// <returns>A list of Results that are displayed in PowerToys Run.</returns>
        private List<Result> FetchBangs(string search) {
            // Create an empty list of results
            List<Result> results = new List<Result>();
            
            // Set defualt sub title
            string subTitle = "Update the list of bangs.";

            // Set the default number of top sites to include in the list
            int rankingLimit = 0;

            // If search is not empty, then update the sub title
            // - Will indicate to the user how many top sites will be included in the list
            if (!string.IsNullOrEmpty(search)) {
                // Check if search is a valid integer, otherwise default to 0
                if (!int.TryParse(search, out rankingLimit)) rankingLimit = 0;
            }

            // If rating limit is greater than 0, update the sub title
            if (rankingLimit > 0) subTitle = $"Update the list of bangs with top {search} sites.";

            results.Add(new Result {
                IcoPath = IconPath,
                Title = "Update Bangs",
                SubTitle = $"{subTitle} This will override all default bangs.",
                Action = e => {
                    // Update the list of bangs with the specified ranking limit
                    FetchBangs(rankingLimit);

                    // Update the list of Bangs
                    LoadBangs();

                    // Clear the search box
                    // - This is useful since the bang has already been executed
                    Context.API.ChangeQuery("", true);

                    // Return true to indicate that the action was successful
                    return true;
                }
            });

            // Return the list of results
            return results;
        }

        /// <summary>
        ///  Updates the list of bangs by running a PowerShell script.
        ///  The PowerShell script generates a list of bangs based on the user's browsing history.
        ///  The top parameter specifies the number of top sites to include in the list.
        /// </summary>
        /// <param name="top"></param>
        public void FetchBangs(int top = 0) {
            // URL to fetch data from
            string url = "https://duckduckgo.com/bang.js";

            try {
                // Fetch the data from the URL
                using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient()) {
                    Context.API.ShowNotification("Updating Bangs...", "This may take a few seconds");
                    string responseContent = client.GetStringAsync(url).Result;

                    // Parse the JSON data
                    System.Text.Json.Nodes.JsonArray jsonData = System.Text.Json.Nodes.JsonNode.Parse(responseContent).AsArray();

                    // Sort and filter the data based on the 'r' property
                    var filteredData = top > 0
                        ? jsonData.OrderByDescending(x => x["r"]?.GetValue<double>() ?? 0).Take(top).ToList()
                        : jsonData.ToList();

                    // Transform the data
                    var transformedData = filteredData.Select(item => new Bang {
                        Trigger = item["t"]?.ToString(),
                        Title = item["s"]?.ToString(),
                        SubTitle = $"{item["c"]?.ToString()} - {item["sc"]?.ToString()}",
                        Urls = new List<string> { (item["u"]?.ToString() ?? "").Replace("{{{s}}}", "%s") }
                    }).ToList();

                    // Serialize the transformed data back to JSON
                    string outputJson = System.Text.Json.JsonSerializer.Serialize(transformedData, new System.Text.Json.JsonSerializerOptions {
                        WriteIndented = false // Use true if you want readable JSON
                    });

                    // Save the JSON to a file
                    string assemblyDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string outputFile = System.IO.Path.Combine(assemblyDirectory, "Bangs.json");
                    System.IO.File.WriteAllText(outputFile, outputJson);
                }
            } catch (Exception ex) {
                // Handle any exceptions
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///  Creates a result to reload the list of bangs from the JSON files.
        /// </summary>
        /// <param name="search">The search term to use with the bang.</param>
        /// <returns>A list of Results that are displayed in PowerToys Run.</returns>
        private List<Result> ReloadBangs(string search) {
            // Create an empty list of results
            List<Result> results = new List<Result>();

            // Add a result to prompt the user to enter a Title and SubTitle for the new bang
            results.Add(new Result {
                IcoPath = IconPath,
                Title = "Reload Bangs",
                SubTitle = "Reload the list of bangs from the JSON files.",
                Action = e => {
                    // Reload the list of bangs from the JSON files
                    ReloadBangs();

                    // Clear the search box
                    // - This is useful since the bang has already
                    Context.API.ChangeQuery("", true);

                    // Return true to indicate that the action was successful
                    return true;
                }
            });

            // Return the list of results
            return results;
        }

        /// <summary>
        ///   Reloads the list of bangs from the JSON files.
        /// </summary>
        public void ReloadBangs() {
            // Load the Bangs from JSON
            LoadBangs();
        }

        /// <summary>
        ///   Saves a new bang to the list of Custom Bangs.
        ///   The new bang is added to the "Custom.json" file in the plugin's assembly directory.
        /// </summary>
        /// <param name="bang">The new bang to save.</param>
        private void SaveBang(Bang bang) {
            // Get the directory of the executing assembly
            string assemblyDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // Combine the directory with the filename to get the full path
            string filePath = System.IO.Path.Combine(assemblyDirectory, "Custom.json");

            // Load the existing custom bangs
            List<Bang> customBangs = GetBangs("Custom.json");

            // Check if the new Bang trigger already exists in the list of custom bangs
            if (customBangs.Exists(b => b.Trigger.ToLower() == bang.Trigger.ToLower())) {
                // Check if the URL already exists in the list of URLs for the specified trigger
                if (customBangs.Find(b => b.Trigger.ToLower() == bang.Trigger.ToLower()).Urls.Contains(bang.Urls[0])) {
                    // If trigger and url already exists, return
                    Context.API.ShowNotification("Bang already exists", "The specified bang already exists in the list of custom bangs.");
                    return;
                }

                // Add the URL to the list of URLs for the specified trigger
                customBangs.Find(b => b.Trigger.ToLower() == bang.Trigger.ToLower()).Urls.Add(bang.Urls[0]);
            }else{
                // Add the new bang to the list of custom
                customBangs.Add(bang);
            }

            // Serialize the custom bangs to JSON
            string outputJson = System.Text.Json.JsonSerializer.Serialize(customBangs, new System.Text.Json.JsonSerializerOptions {
                WriteIndented = false // Use true if you want readable JSON
            });
            // Save the JSON to a file
            System.IO.File.WriteAllText(filePath, outputJson);

            // Update the list of Bangs
            LoadBangs();
        }

        /// <summary>
        ///    Removes a bang from its list of bangs.
        ///    The bang will be removed from either the predefined or custom list of bangs.
        /// </summary>
        /// <param name="trigger">The trigger for the bang to remove.</param>
        /// <param name="url">The trigger for the bang to remove.</param>
        private void RemoveBang(string trigger, string url) {
            // Get the directory of the executing assembly
            string assemblyDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // Combine the directory with the filename to get the full path
            string bangsPath = System.IO.Path.Combine(assemblyDirectory, "Bangs.json");
            string customPath = System.IO.Path.Combine(assemblyDirectory, "Custom.json");

            // Load the existing bangs
            Dictionary<string, List<Bang>> bangs = new Dictionary<string, List<Bang>> {
                { "Bangs", GetBangs("Bangs.json") },
                { "Custom", GetBangs("Custom.json") }
            };

            // Loop through the Dictionary of bangs
            foreach (var item in bangs) {
                // Check if the trigger exists in the list of predefined bangs
                if (item.Value.Exists(b => b.Trigger.ToLower() == trigger.ToLower())) {
                    // Check if the URL exists in the list of URLs for the specified trigger
                    if (item.Value.Find(b => b.Trigger.ToLower() == trigger.ToLower()).Urls.Contains(url)) {
                        // Remove the URL from the list of URLs for the specified trigger
                        item.Value.Find(b => b.Trigger.ToLower() == trigger.ToLower()).Urls.Remove(url);

                        // Check if the item has no URLs
                        if (item.Value.Find(b => b.Trigger.ToLower() == trigger.ToLower()).Urls.Count == 0) {
                            // Remove the bang from the list of predefined bangs
                            item.Value.RemoveAll(b => b.Trigger.ToLower() == trigger.ToLower());
                        }

                        // Serialize the predefined bangs to JSON
                        string outputJson = System.Text.Json.JsonSerializer.Serialize(item.Value, new System.Text.Json.JsonSerializerOptions {
                            WriteIndented = false // Use true if you want readable JSON
                        });

                        // Save the JSON to a file
                        System.IO.File.WriteAllText(item.Key == "Bangs" ? bangsPath : customPath, outputJson);
                    }
                }
            }
        }

        /// <summary>
        ///   Gets the path to the icon for the specified URL.
        ///   If the icon does not exist, it is downloaded from DuckDuckGo's external-content service.
        ///   The icon is saved to the "Images" directory in the plugin's assembly directory.
        ///   else the default icon is returned.
        /// </summary>
        /// <param name="url">The URL for which to get the icon.</param>
        /// <returns>The path to the icon file.</returns>
        public string GetBangImage(string url, string defaultPath = "") {
            // If DownloadIcons is false, return the default icon
            if (!DownloadIcons) return IconPath;

            // Get the domain from the URL
            string domain = new Uri(url).Host.Replace("www.", "");

            // Special code to use correct domain when searching a site using google.
            // - if the domain is google.com and the url contains `site:xyz.abc` then use site as the domain.
            // - exmaple formats are https://www.google.com/search?q=dialog+site:MudBlazor.com
            if (domain == "google.com" && url.Contains("site:")) domain = url.Split("site:")[1];

            // Check if the icon exists
            // - If the icon does not exist, download it
            // - The icon is downloaded from DuckDuckGo's external-content service
            string assemblyDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string imgPath = System.IO.Path.Combine(assemblyDirectory, "Images", "Ico", $"{domain}.ico");
            
            // If the icon exists, return
            // - This prevents downloading the icon multiple times
            if (System.IO.File.Exists(imgPath)) return imgPath;

            // If Ico directory does not exist, create it
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(assemblyDirectory, "Images", "Ico"))) System.IO.Directory.CreateDirectory(System.IO.Path.Combine(assemblyDirectory, "Images", "Ico"));

            // Download Icon
            try {
                using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient()) {
                    //DuckDuckGo: "https://external-content.duckduckgo.com/i/%s.ico"
                    //Google FavIconV2: "https://t0.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&size=64&url=https://%s"
                    var response = client.GetAsync((String.IsNullOrEmpty(defaultPath) ? FavIconUrl : defaultPath).Replace("%s", domain)).Result;
                    if (!response.IsSuccessStatusCode) throw new Exception("Failed to download icon.");
                    
                    // Ensure the directory exists
                    System.IO.Directory.CreateDirectory("Images");

                    // Save the file
                    // Read content as a stream
                    using (var contentStream = response.Content.ReadAsStreamAsync().Result)
                    using (var fileStream = new  System.IO.FileStream(imgPath,  System.IO.FileMode.Create,  System.IO.FileAccess.Write,  System.IO.FileShare.None)) {
                        contentStream.CopyTo(fileStream); // Synchronously copy the content
                    }
                }
            } catch {
                // If defaultPath is empty and FavIconUrl domain is not to.gstatic.com
                // - Try to download the icon from Google's external-content service https://t0.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&size=64&url=https://%s
                if (String.IsNullOrEmpty(defaultPath)) {
                    string downloadDomain = new Uri(FavIconUrl).Host.Replace("www.", "");
                    if (downloadDomain != "t0.gstatic.com") return GetBangImage(url, "https://t0.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&size=64&url=https://%s");
                }

                // Failed to download the icon
                // - This can happen if the icon does not exist or if there is a network error
                // - In this case, the plugin will use the default icon
                return IconPath;
            }

            return imgPath;
        }



        public void Init(PluginInitContext context) {
            // Store the context for later use
            Context = context ?? throw new ArgumentNullException(nameof(context));

            // Subscribe to the ThemeChanged event
            Context.API.ThemeChanged += OnThemeChanged;
            // Update the icon path based on the current theme
            UpdateIconPath(Context.API.GetCurrentTheme());

            // Load the Bangs from JSON for the first time
            LoadBangs();
        }

        /// <summary>
        /// Creates setting panel.
        /// </summary>
        /// <returns>The control.</returns>
        /// <exception cref="NotImplementedException">method is not implemented.</exception>
        public System.Windows.Controls.Control CreateSettingPanel() => throw new NotImplementedException();

        /// <summary>
        /// Updates settings.
        /// </summary>
        /// <param name="settings">The plugin settings.</param>
        public void UpdateSettings(PowerLauncherPluginSettings settings) {
            FavIconUrl = settings.AdditionalOptions.SingleOrDefault(x => x.Key == nameof(FavIconUrl))?.TextValue ?? FavIconUrl;
            DownloadIcons = settings.AdditionalOptions.SingleOrDefault(x => x.Key == nameof(DownloadIcons))?.Value ?? DownloadIcons;
        }
        

        private void UpdateIconPath(Theme theme) => IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite ? "Images/bangsearch.light.png" : "Images/bangsearch.dark.png";

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);
    }
}
