<div style="text-align: center;">
    <img src="logo.png" alt="BangSearch" height="200px" />
    <h1 style="border-width:0;font-size: 5rem;font-weight:900;margin-bottom:0;padding-bottom:0">BangSearch</h1>
    <h2 style="border-width:0;margin-top:0">Plugin for PowerToys Run</h2>
</div>

## Overview
The BangSearch plugin for PowerToys Run allows users to quickly search the web using predefined search shortcuts (bangs). This plugin enhances productivity by enabling fast and efficient web searches directly from the PowerToys Run interface.

This feature is inspired by the amazing service provided by DuckDuckGo, which offers privacy-focused web searches. This project is an independent development and is not affiliated with DuckDuckGo. You should totally check them out at their [DuckDuckGo Homepage](https://duckduckgo.com/?origin=funnel_help)!

## Features
- **Customizable Bangs**: Define your own search shortcuts.
- **Multiple Search Engines**: Support for various search engines.
- **Fast and Efficient**: Quickly perform web searches without opening a browser first.

## Installation
The recommended path for the plugins is `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`

1. Close PowerToys.
2. Download the Latest Release of BangSearch
3. Copy the plugin folder to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`
4. Open PowerToys
5. Go to PowerToys Run
6. Enable BangSearch

## Usage
1. Open PowerToys Run (Alt + Space).
2. Type a bang followed by your search query (e.g., `!yt Cool Programming Stuff`).
3. Press Enter to perform the search.

> **Warning**: This plugin downloads icons from the web to display inside of PowerToys Run. This can cause the plugin to take up more space depending on the number of **!bangs** you have. You can disable this setting in the settings if you would like to,

## Features
### Add Bang Documentation
The `!add` function is used to parse a search string and extract key components to create a `Result` object. This function allows users to define custom "bangs" with specific triggers, optional titles, subtitles, and URLs.

#### How It Works
1. **Input Parsing**:
   - The input `search` string is parsed using a regular expression.
   - It extracts the following components:
     - **Trigger**: A mandatory keyword to identify the bang.
     - **Title** (Optional): A descriptive title for the bang.
     - **Subtitle** (Optional): A short subtitle for additional context.
     - **URL**: A mandatory URL associated with the bang.

2. **Validation**:
   - If the `trigger` or `url` is missing, the function returns an empty list of results.

3. **Result Creation**:
   - If the parsing succeeds and all mandatory components are present, the function creates a list of `Result` objects using the extracted information.

##### Regular Expression Breakdown

The regular expression used to parse the `search` string is: `^(?<trigger>\S+)(?:\s+(?<title>[^\|]+?))?(?:\s+\|\s+(?<subtitle>[^\s]+))?\s+(?<url>https?://\S+)$`

- `(?<trigger>\S+)`: Captures the trigger (a single word or keyword).
- `(?:\s+(?<title>[^\|]+?))?`: Optionally captures the title, separated by spaces.
- `(?:\s+\|\s+(?<subtitle>[^\s]+))?`: Optionally captures the subtitle, prefixed by a pipe (`|`) and separated by spaces.
- `(?<url>https?://\S+)`: Captures the URL starting with `http://` or `https://`.

#### Examples
Here are some example inputs and their expected behavior:

>#### Example 1: Trigger and URL Only
>"!add search https://www.google.com/search?q=%s"
>
> Output:
>- **Trigger**: `!search`
>- **URL**: `https://www.google.com/search?q=%s`
>- **Result**: A single result is created with the trigger and URL.

>#### Example 2: Trigger, Title, and URL
>"!add g Google Search https://www.google.com/search?q=%s"
>
> Output:
>- **Trigger**: `!g`
>- **Title**: `Google Search`
>- **URL**: `https://www.google.com/search?q=%s`
>- **Result**: A single result is created with the trigger, title, and URL.

>#### Example 3: Trigger, Title, Subtitle, and URL
>"!add wiki Wikipedia | Free Encyclopedia https://en.wikipedia.org/wiki/Special:Search?search=%s"
>
> Output:
>- **Trigger**: `!wiki`
>- **Title**: `Wikipedia`
>- **Subtitle**: `Free Encyclopedia`
>- **URL**: `https://en.wikipedia.org/wiki/Special:Search?search=%s`
>- **Result**: A single result is created with all components.

#### Usage Notes
The trigger and URL are mandatory components; missing these will result in no results being created. Also the url must use `%s` where you would like the search query to be placed.

### Remove Bang Documentation
The `!remove` function is used to remove a bang from the search system.

To remove a bang, the user types out `!remove` followed by the trigger word and then selects one of the results.
This will remove the bang URL associated with the trigger word. If there are no URLs left associated with the trigger word, the trigger word itself will also be removed.

>#### Example:
>"!remove mtg"
>A list of results will be displayed, you simply select the one you want to remove and it will be removed.

### Fetch Bang Documentation
The `!fetch` function allows users to retrieve a list of bangs from DuckDuckGo, sorted by ranking.

#### How It Works
1. **Input Parsing**:
   - The input `count` specifies the number of top bangs to fetch.
   - If `count` is `0`, all bangs will be fetched.

2. **Fetching Data**:
   - The function sends a request to DuckDuckGo to retrieve the bangs.
   - The bangs are sorted by their ranking.

3. **Overwriting Bangs**:
   - The fetched bangs will overwrite the current `Bangs.json` file.
   - This file is handcrafted to work best with PowerToys Run, so use this feature with caution.

#### Examples
Here are some example inputs and their expected behavior:

>#### Example 1: Fetch Top 100 Bangs
>"!fetch 100"
>
> Output:
>- Fetches the top 100 bangs from DuckDuckGo.
>- Overwrites the current `Bangs.json` file with the fetched bangs.

>#### Example 2: Fetch All Bangs
>"!fetch 0"
>
> Output:
>- Fetches all bangs from DuckDuckGo.
>- Overwrites the current `Bangs.json` file with the fetched bangs.

#### Usage Notes
Using the `!fetch` command will overwrite the existing `Bangs.json` file. Ensure you have a backup of your current bangs if you wish to restore them later.

### Reload Bangs Documentation
The `!reload` command is used to regrab the data from the `Bangs.json` and `Custom.json` files. This feature is particularly useful if you have manually changed one of these JSON files and do not want to restart the plugin or PowerToys Run to reload the changes.

#### Usage:
- Type `!reload` in the command input to trigger the reload process.

>#### Example:
>"!reload"

After executing the `!reload` command, the latest data from `Bangs.json` and `Custom.json` will be loaded, reflecting any manual changes made to these files.

## Configuration
To customize your bangs:
1. Open the configuration file located at `path/to/config`.
2. Add or modify the bang definitions as needed.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Attribution
The icon used in this project is from Mozilla's FxEmojis library. I make no claim to ownership of this icon. 
The license for the FxEmojis library can be found at: [Mozilla / FxEmoji - License](https://github.com/mozilla/fxemoji/blob/gh-pages/LICENSE.md).

## Contact
For any questions or suggestions, please open an issue on the [GitHub repository](https://github.com/mouse0270/BangSearch).
