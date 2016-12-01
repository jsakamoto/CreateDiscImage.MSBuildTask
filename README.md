# CreateDiscImage.MSBuildTask

[![NuGet Package](https://img.shields.io/nuget/v/CreateDiscImage.MSBuildTask.svg)](https://www.nuget.org/packages/CreateDiscImage.MSBuildTask/)


## Summary

This is a NuGet package for Visual Studio MSBuild task.

Just by installing this package, a disc image file (.iso) that contains the output files will be created in the output folder when building the project (at default configuration, it works only "Release" build).

This MSBuild task support both UDF and CDFS(ISO9660) format.

## Install

    PM> Install-Package CreateDiscImage.MSBuildTask

## How to configure?

Add "CreateDiscImage.props" file to the project like bellow.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <DiscImagePath>$(OutputPath)$(TargetName).iso</DiscImagePath>
    <DiscImageVolumeLabel></DiscImageVolumeLabel>
    <!-- DiscImageFileSystemType = {ISO9660|UDF} -->
    <DiscImageFileSystemType>ISO9660</DiscImageFileSystemType>
    <DiscImageRemoveRoots>$(OutputPath)</DiscImageRemoveRoots>
    <DiscImageUseJoliet>true</DiscImageUseJoliet>
    <!-- CreateDiscImageConfiguration = ex)"Release;Debug" -->
    <CreateDiscImageConfiguration>Release</CreateDiscImageConfiguration>
  </PropertyGroup>
  <Target Name="CustomDiscImageSourceFiles" BeforeTargets="CreateDiscImage">
    <PropertyGroup>
      <DisableDefaultDiscImageSourceFiles>true</DisableDefaultDiscImageSourceFiles>
    </PropertyGroup>
    <ItemGroup>
      <DiscImageSourceFiles Include="$(OutputPath)**\*" Exclude="**\*.iso;**\*.vshost.exe;**\*.vshost.exe.manifest" />
    </ItemGroup>
  </Target>
</Project>
```

"CreadteDiscImage" Build task read "CreateDiscImage.props" if it exists, and use those properties high priority.

## license

[Mozilla Public License Version 2.0](lICENSE)