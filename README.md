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

## Install
To install this extension, you will need:
  1. Visual Studio version 2022 or 2019
  2. The `Tanzu.Toolkit.VisualStudio.vsix` extension file, downloadable from any of these sources:
      - The Visual Studio Marketplace:
        - through the IDE by searching "Tanzu" in the "Manage Extensions" window
        - through the website: [VS 2022](https://marketplace.visualstudio.com/items?itemName=TanzuNETExperience.TanzuToolkitForVisualStudio2022), [VS 2019](https://marketplace.visualstudio.com/items?itemName=TanzuNETExperience.TanzuToolkitForVisualStudio2019)
      - vsixgallery.com: [VS 2022](https://www.vsixgallery.com/extension/TanzuToolkitForVisualStudio2022.ff7b6f3e-0410-4ff9-a40a-a719ee9da901), [VS 2019](https://www.vsixgallery.com/extension/TanzuToolkitForVisualStudio2019.ff7b6f3e-0410-4ff9-a40a-a719ee9da901)
      - The ["Releases" section](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/releases) of this repository
        - look for the `.vsix` file under "Assets" that corresponds to your version of Visual Studio

Once you've downloaded `Tanzu.Toolkit.VisualStudio.vsix`, double-click it to prompt an installation into your instance of Visual Studio.

## Uninstall
To uninstall this extension from Visual Studio, visit the "Extension Manager" found under the `Extensions` menu.

![image](https://user-images.githubusercontent.com/22666145/168169965-14855a9f-2f8c-458e-ad24-d50f1d8f1b24.png)
![image](https://user-images.githubusercontent.com/22666145/168169970-969cf089-2028-433c-82d5-55a67afb7fd0.png)

## More Info
Check out our [Wiki](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/wiki)! It goes into more detail on several features.

Our team uses [this board in ZenHub](https://app.zenhub.com/workspaces/net-dev-x---visual-studio-extensions-604161e65a9f390012665e4d/board?repos=327998348) to track progress.

## Demo videos

### Overview

https://user-images.githubusercontent.com/22666145/153500532-c1c5b322-3a9c-40ad-8d7f-a479fa8a5f36.mp4

### App Deployment using a Manifest

https://user-images.githubusercontent.com/22666145/144128093-8d1686c3-eac1-4bf1-baaa-8eb7c262f5d0.mp4

### Deploying 'Published' .NET apps

https://user-images.githubusercontent.com/22666145/144897999-087c5a76-b844-4bb4-9e33-dabd4d42210f.mp4

### Remote Debugging .NET apps running on Tanzu Application Service

https://user-images.githubusercontent.com/22666145/161144428-eb695444-39c1-4bb3-93c9-996f81919678.mp4

