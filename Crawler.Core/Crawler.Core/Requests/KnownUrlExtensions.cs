using System;
using System.Collections.Generic;

public static class FileExtensions
{
    public static readonly HashSet<string> KnownFileExtensions = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        // Documents
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".rtf",
        // Archives
        ".zip",
        ".rar",
        ".tar",
        ".gz",
        ".7z",
        // Data / Code
        ".json",
        ".xml",
        ".csv",
        ".yaml",
        ".yml",
    };
}
