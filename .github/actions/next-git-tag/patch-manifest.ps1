<#
    .SYNOPSIS
    This script modifies the extension.vsixmanifest file by appending "preview" to the DisplayName when it's a release.
#>

Param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,  # Path to extension.vsixmanifest
    [Parameter(Mandatory = $true)]
    [string]$PipelineType   # Type of pipeline: "preview" or "stable"
)

# Load the XML file
[xml]$xml = Get-Content -Path $ManifestPath

# Check the pipeline type, and if it's a release, modify the DisplayName
if ($PipelineType -eq "preview") {
    Write-Host "Pipeline is release, appending 'preview' to DisplayName."
    $displayNameNode = $xml.PackageManifest.Metadata.DisplayName
    # Append ' preview' to the existing DisplayName
    $displayNameNode = "(Preview) $displayNameNode"
    $xml.PackageManifest.Metadata.DisplayName = $displayNameNode
    $xml.PackageManifest.Metadata.Preview = "true"
    
    Write-Host "Updated DisplayName: $displayNameNode"
    Write-Host "Updated Preview: true"

} else {
    Write-Host "Pipeline is not preview, no changes made to DisplayName."
}

# Save the modified XML back to the file
$xml.Save($ManifestPath)
$xml.OuterXml | Write-Host
Write-Host "Manifest file has been updated successfully."
