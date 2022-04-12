<img src="https://dtb5pzswcit1e.cloudfront.net/assets/images/product_logos/icon_elastic-runtime_1586547456.png" alt="VMware Tanzu logo" height="100" align="left"/>

# Tanzu Toolkit for Visual Studio

[![Build Status](https://dev.azure.com/TanzuDevX/DevX/_apis/build/status/Build%2C%20Test%20%26%20Package%20VSIX?branchName=main)](https://dev.azure.com/TanzuDevX/DevX/_build/latest?definitionId=3&branchName=main)

Tanzu Toolkit for Visual Studio is an extension for Visual Studio (2022 & 2019) that allows users of Tanzu Application Service ("TAS") to manage apps directly from within the IDE. Users of this extension are able to:

- Manage TAS environments with the [Tanzu Application Service Explorer](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/wiki/Tanzu-Application-Service-Explorer)
  - View orgs, spaces, and apps
  - Start / stop / delete apps
  - Tail app logs
- Deploy apps from Visual Studio using the [App Deployment window](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/wiki/App-Deployment-Window)
  - Use / create Cloud Foundry manifest files for consistent app deployments
- [Remotely debug](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/wiki/Remote-Debugging-TAS-Apps) apps running on TAS within Visual Studio

### Overview

https://user-images.githubusercontent.com/22666145/153500532-c1c5b322-3a9c-40ad-8d7f-a479fa8a5f36.mp4

### App Deployment using a Manifest

https://user-images.githubusercontent.com/22666145/144128093-8d1686c3-eac1-4bf1-baaa-8eb7c262f5d0.mp4

### Deploying 'Published' .NET apps

https://user-images.githubusercontent.com/22666145/144897999-087c5a76-b844-4bb4-9e33-dabd4d42210f.mp4

### Remote Debugging .NET apps running on Tanzu Application Service

https://user-images.githubusercontent.com/22666145/161144428-eb695444-39c1-4bb3-93c9-996f81919678.mp4


## Install
- To install this extension, you will need Visual Studio 2022 or 2019.
  - VS 2022
    - You can find a pre-release under the `v0.0.4` tag in the ["Releases" section of this repository](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/releases).
  - VS 2019
    - You can find a pre-release under the `v0.0.4` tag in the ["Releases" section of this repository](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/releases).
    - The latest build can be found [here on vsixgallery.com](https://www.vsixgallery.com/extension/TanzuToolkitForVisualStudio.ff7b6f3e-0410-4ff9-a40a-a719ee9da901)
- Download the `Tanzu.Toolkit.VisualStudio.vsix` file & double-click to prompt an installation into your Visual Studio instance.
- To uninstall this extension from Visual Studio, visit the Visual Studio Extension Manager found under the `Extensions` menu.

## More Info
Check out our [Wiki](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/wiki)!

Our team uses [this board in ZenHub](https://app.zenhub.com/workspaces/net-dev-x---visual-studio-extensions-604161e65a9f390012665e4d/board?repos=327998348) to track progress.
