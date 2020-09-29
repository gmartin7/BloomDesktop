#!/bin/bash
# server=build.palaso.org
# project=Bloom
# build=Bloom-4.8-CSInternal-Continuous
# root_dir=..
# Auto-generated by https://github.com/chrisvire/BuildUpdate.
# Do not edit this file by hand!

cd "$(dirname "$0")"

# *** Functions ***
force=0
clean=0

while getopts fc opt; do
case $opt in
f) force=1 ;;
c) clean=1 ;;
esac
done

shift $((OPTIND - 1))

copy_auto() {
if [ "$clean" == "1" ]
then
echo cleaning $2
rm -f ""$2""
else
where_curl=$(type -P curl)
where_wget=$(type -P wget)
if [ "$where_curl" != "" ]
then
copy_curl "$1" "$2"
elif [ "$where_wget" != "" ]
then
copy_wget "$1" "$2"
else
echo "Missing curl or wget"
exit 1
fi
fi
}

copy_curl() {
echo "curl: $2 <= $1"
if [ -e "$2" ] && [ "$force" != "1" ]
then
curl -# -L -z "$2" -o "$2" "$1"
else
curl -# -L -o "$2" "$1"
fi
}

copy_wget() {
echo "wget: $2 <= $1"
f1=$(basename $1)
f2=$(basename $2)
cd $(dirname $2)
wget -q -L -N "$1"
# wget has no true equivalent of curl's -o option.
# Different versions of wget handle (or not) % escaping differently.
# A URL query is the only reason why $f1 and $f2 should differ.
if [ "$f1" != "$f2" ]; then mv $f2\?* $f2; fi
cd -
}


# *** Results ***
# build: Bloom-4.8-CSInternal-Continuous (Bloom_Bloom48CSInternalContinuous)
# project: Bloom
# URL: https://build.palaso.org/viewType.html?buildTypeId=Bloom_Bloom48CSInternalContinuous
# VCS: git://github.com/BloomBooks/BloomDesktop.git [Version4.8_CS]
# dependencies:
# [0] build: bloom-win32-static-dependencies (bt396)
#     project: Bloom
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt396
#     clean: false
#     revision: bloom-4.8-cs.tcbuildtag
#     paths: {"ghostscript-win32.zip!**"=>"DistFiles/ghostscript", "optipng-0.7.4-win32/optipng.exe"=>"DistFiles", "connections.dll"=>"DistFiles", "MSBuild.Community.Tasks.dll"=>"build", "MSBuild.Community.Tasks.Targets"=>"build", "Lame.zip!**"=>"lib/lame", "FirefoxDependencies.zip!**"=>"lib/FirefoxDependencies"}
# [1] build: PortableDevices (from PodcastUtilities) (Bloom_PortableDevicesFromPodcastUtitlies)
#     project: Bloom
#     URL: https://build.palaso.org/viewType.html?buildTypeId=Bloom_PortableDevicesFromPodcastUtitlies
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"PodcastUtilities.PortableDevices.dll"=>"lib/dotnet", "PodcastUtilities.PortableDevices.pdb"=>"lib/dotnet", "Interop.PortableDeviceApiLib.dll"=>"lib/dotnet", "Interop.PortableDeviceTypesLib.dll"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/PodcastUtilities.git [refs/heads/master]
# [2] build: Squirrel (Bloom_Squirrel)
#     project: Bloom
#     URL: https://build.palaso.org/viewType.html?buildTypeId=Bloom_Squirrel
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"*.*"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/Squirrel.Windows.git [refs/heads/master]
# [3] build: Bloom Help 4.8 (Bloom_Help_BloomHelp48)
#     project: Help
#     URL: https://build.palaso.org/viewType.html?buildTypeId=Bloom_Help_BloomHelp48
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"*.chm"=>"DistFiles"}
# [4] build: pdf.js (bt401)
#     project: BuildTasks
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt401
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"pdfjs-viewer.zip!**"=>"DistFiles/pdf"}
#     VCS: https://github.com/mozilla/pdf.js.git [gh-pages]
# [5] build: GeckofxHtmlToPdf-Win32-Geckofx60-continuous (GeckofxHtmlToPdf_GeckofxHtmlToPdfGeckofx60Win32continuous)
#     project: GeckofxHtmlToPdf
#     URL: https://build.palaso.org/viewType.html?buildTypeId=GeckofxHtmlToPdf_GeckofxHtmlToPdfGeckofx60Win32continuous
#     clean: false
#     revision: bloom-4.8-cs.tcbuildtag
#     paths: {"Args.dll"=>"lib/dotnet", "GeckofxHtmlToPdf.exe"=>"lib/dotnet", "GeckofxHtmlToPdf.exe.config"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/geckofxHtmlToPdf [Geckofx60]
# [6] build: L10NSharp master continuous (L10NSharp_L10NSharpMasterContinuous)
#     project: L10NSharp
#     URL: https://build.palaso.org/viewType.html?buildTypeId=L10NSharp_L10NSharpMasterContinuous
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"L10NSharp.dll"=>"lib/dotnet/", "L10NSharp.pdb"=>"lib/dotnet/", "CheckOrFixXliff.exe"=>"lib/dotnet/"}
#     VCS: https://github.com/sillsdev/l10nsharp [refs/heads/master]
# [7] build: NAudio continuous (bt402)
#     project: NAudio
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt402
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"NAudio.dll"=>"lib/dotnet"}
#     VCS: https://github.com/sillsdev/naudio.git [master]
# [8] build: PdfDroplet-Win-master-Continuous (bt54)
#     project: PdfDroplet
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt54
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"PdfDroplet.exe"=>"lib/dotnet", "PdfSharp.dll"=>"lib/dotnet"}
#     VCS: https://github.com/sillsdev/pdfDroplet [master]
# [9] build: TidyManaged-master-win32-continuous (bt349)
#     project: TidyManaged
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt349
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"*.*"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/TidyManaged.git [master]
# [10] build: Windows master continuous (XliffForHtml_WindowsMasterContinuous)
#     project: XliffForHtml
#     URL: https://build.palaso.org/viewType.html?buildTypeId=XliffForHtml_WindowsMasterContinuous
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"HtmlXliff.*"=>"lib/dotnet", "HtmlAgilityPack.*"=>"lib/dotnet"}
#     VCS: https://github.com/sillsdev/XliffForHtml [refs/heads/master]
# [11] build: palaso-win32-master-nostrongname Continuous (Libpalaso_PalasoWin32masterNostrongnameContinuous)
#     project: libpalaso
#     URL: https://build.palaso.org/viewType.html?buildTypeId=Libpalaso_PalasoWin32masterNostrongnameContinuous
#     clean: false
#     revision: bloom-4.8.tcbuildtag
#     paths: {"irrKlang.NET4.dll"=>"lib/dotnet/", "Newtonsoft.Json.dll"=>"lib/dotnet/", "SIL.Core.dll"=>"lib/dotnet/", "SIL.Core.Desktop.dll"=>"lib/dotnet/", "SIL.Media.dll"=>"lib/dotnet/", "SIL.TestUtilities.dll"=>"lib/dotnet/", "SIL.Windows.Forms.dll"=>"lib/dotnet/", "SIL.Windows.Forms.GeckoBrowserAdapter.dll"=>"lib/dotnet/", "SIL.Windows.Forms.Keyboarding.dll"=>"lib/dotnet/", "SIL.Windows.Forms.WritingSystems.dll"=>"lib/dotnet/", "SIL.WritingSystems.dll"=>"lib/dotnet/", "taglib-sharp.dll"=>"lib/dotnet/"}
#     VCS: https://github.com/sillsdev/libpalaso.git [refs/heads/master]

# make sure output directories exist
mkdir -p ../DistFiles
mkdir -p ../DistFiles/ghostscript
mkdir -p ../DistFiles/pdf
mkdir -p ../Downloads
mkdir -p ../build
mkdir -p ../lib/FirefoxDependencies
mkdir -p ../lib/dotnet
mkdir -p ../lib/dotnet/
mkdir -p ../lib/lame

# download artifact dependencies
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/bloom-4.8-cs.tcbuildtag/ghostscript-win32.zip ../Downloads/ghostscript-win32.zip
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/bloom-4.8-cs.tcbuildtag/optipng-0.7.4-win32/optipng.exe ../DistFiles/optipng.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/bloom-4.8-cs.tcbuildtag/connections.dll ../DistFiles/connections.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/bloom-4.8-cs.tcbuildtag/MSBuild.Community.Tasks.dll ../build/MSBuild.Community.Tasks.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/bloom-4.8-cs.tcbuildtag/MSBuild.Community.Tasks.Targets ../build/MSBuild.Community.Tasks.Targets
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/bloom-4.8-cs.tcbuildtag/Lame.zip ../Downloads/Lame.zip
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/bloom-4.8-cs.tcbuildtag/FirefoxDependencies.zip ../Downloads/FirefoxDependencies.zip
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_PortableDevicesFromPodcastUtitlies/bloom-4.8.tcbuildtag/PodcastUtilities.PortableDevices.dll ../lib/dotnet/PodcastUtilities.PortableDevices.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_PortableDevicesFromPodcastUtitlies/bloom-4.8.tcbuildtag/PodcastUtilities.PortableDevices.pdb ../lib/dotnet/PodcastUtilities.PortableDevices.pdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_PortableDevicesFromPodcastUtitlies/bloom-4.8.tcbuildtag/Interop.PortableDeviceApiLib.dll ../lib/dotnet/Interop.PortableDeviceApiLib.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_PortableDevicesFromPodcastUtitlies/bloom-4.8.tcbuildtag/Interop.PortableDeviceTypesLib.dll ../lib/dotnet/Interop.PortableDeviceTypesLib.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/7z.dll ../lib/dotnet/7z.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/7z.exe ../lib/dotnet/7z.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/DeltaCompressionDotNet.MsDelta.dll ../lib/dotnet/DeltaCompressionDotNet.MsDelta.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/DeltaCompressionDotNet.PatchApi.dll ../lib/dotnet/DeltaCompressionDotNet.PatchApi.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/DeltaCompressionDotNet.dll ../lib/dotnet/DeltaCompressionDotNet.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Microsoft.Data.Edm.dll ../lib/dotnet/Microsoft.Data.Edm.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Microsoft.Data.Edm.xml ../lib/dotnet/Microsoft.Data.Edm.xml
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Microsoft.Data.OData.dll ../lib/dotnet/Microsoft.Data.OData.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Microsoft.Data.OData.xml ../lib/dotnet/Microsoft.Data.OData.xml
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Microsoft.Data.Services.Client.dll ../lib/dotnet/Microsoft.Data.Services.Client.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Microsoft.Data.Services.Client.xml ../lib/dotnet/Microsoft.Data.Services.Client.xml
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Microsoft.Web.XmlTransform.dll ../lib/dotnet/Microsoft.Web.XmlTransform.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Mono.Cecil.dll ../lib/dotnet/Mono.Cecil.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/NuGet.Squirrel.dll ../lib/dotnet/NuGet.Squirrel.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Setup.exe ../lib/dotnet/Setup.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/SharpCompress.dll ../lib/dotnet/SharpCompress.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Splat.dll ../lib/dotnet/Splat.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Squirrel.dll ../lib/dotnet/Squirrel.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/StubExecutable.exe ../lib/dotnet/StubExecutable.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/SyncReleases.exe ../lib/dotnet/SyncReleases.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/System.Spatial.dll ../lib/dotnet/System.Spatial.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/System.Spatial.xml ../lib/dotnet/System.Spatial.xml
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Update-Mono.exe ../lib/dotnet/Update-Mono.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Update.com ../lib/dotnet/Update.com
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/Update.exe ../lib/dotnet/Update.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/WriteZipToSetup.exe ../lib/dotnet/WriteZipToSetup.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/rcedit.exe ../lib/dotnet/rcedit.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-4.8.tcbuildtag/signtool.exe ../lib/dotnet/signtool.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Help_BloomHelp48/latest.lastSuccessful/Bloom.chm ../DistFiles/Bloom.chm
copy_auto http://build.palaso.org/guestAuth/repository/download/bt401/bloom-4.8.tcbuildtag/pdfjs-viewer.zip ../Downloads/pdfjs-viewer.zip
copy_auto http://build.palaso.org/guestAuth/repository/download/GeckofxHtmlToPdf_GeckofxHtmlToPdfGeckofx60Win32continuous/bloom-4.8-cs.tcbuildtag/Args.dll ../lib/dotnet/Args.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/GeckofxHtmlToPdf_GeckofxHtmlToPdfGeckofx60Win32continuous/bloom-4.8-cs.tcbuildtag/GeckofxHtmlToPdf.exe ../lib/dotnet/GeckofxHtmlToPdf.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/GeckofxHtmlToPdf_GeckofxHtmlToPdfGeckofx60Win32continuous/bloom-4.8-cs.tcbuildtag/GeckofxHtmlToPdf.exe.config ../lib/dotnet/GeckofxHtmlToPdf.exe.config
copy_auto http://build.palaso.org/guestAuth/repository/download/L10NSharp_L10NSharpMasterContinuous/bloom-4.8.tcbuildtag/L10NSharp.dll ../lib/dotnet/L10NSharp.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/L10NSharp_L10NSharpMasterContinuous/bloom-4.8.tcbuildtag/L10NSharp.pdb ../lib/dotnet/L10NSharp.pdb
copy_auto http://build.palaso.org/guestAuth/repository/download/L10NSharp_L10NSharpMasterContinuous/bloom-4.8.tcbuildtag/CheckOrFixXliff.exe ../lib/dotnet/CheckOrFixXliff.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/bt402/bloom-4.8.tcbuildtag/NAudio.dll ../lib/dotnet/NAudio.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt54/bloom-4.8.tcbuildtag/PdfDroplet.exe ../lib/dotnet/PdfDroplet.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/bt54/bloom-4.8.tcbuildtag/PdfSharp.dll ../lib/dotnet/PdfSharp.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt349/bloom-4.8.tcbuildtag/TidyManaged.dll ../lib/dotnet/TidyManaged.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt349/bloom-4.8.tcbuildtag/TidyManaged.dll.config ../lib/dotnet/TidyManaged.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/bt349/bloom-4.8.tcbuildtag/libtidy.dll ../lib/dotnet/libtidy.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/XliffForHtml_WindowsMasterContinuous/bloom-4.8.tcbuildtag/HtmlXliff.exe ../lib/dotnet/HtmlXliff.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/XliffForHtml_WindowsMasterContinuous/bloom-4.8.tcbuildtag/HtmlXliff.pdb ../lib/dotnet/HtmlXliff.pdb
copy_auto http://build.palaso.org/guestAuth/repository/download/XliffForHtml_WindowsMasterContinuous/bloom-4.8.tcbuildtag/HtmlAgilityPack.dll ../lib/dotnet/HtmlAgilityPack.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/irrKlang.NET4.dll ../lib/dotnet/irrKlang.NET4.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/Newtonsoft.Json.dll ../lib/dotnet/Newtonsoft.Json.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.Core.dll ../lib/dotnet/SIL.Core.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.Core.Desktop.dll ../lib/dotnet/SIL.Core.Desktop.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.Media.dll ../lib/dotnet/SIL.Media.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.TestUtilities.dll ../lib/dotnet/SIL.TestUtilities.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.Windows.Forms.dll ../lib/dotnet/SIL.Windows.Forms.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.Windows.Forms.GeckoBrowserAdapter.dll ../lib/dotnet/SIL.Windows.Forms.GeckoBrowserAdapter.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.Windows.Forms.Keyboarding.dll ../lib/dotnet/SIL.Windows.Forms.Keyboarding.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.Windows.Forms.WritingSystems.dll ../lib/dotnet/SIL.Windows.Forms.WritingSystems.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/SIL.WritingSystems.dll ../lib/dotnet/SIL.WritingSystems.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoWin32masterNostrongnameContinuous/bloom-4.8.tcbuildtag/taglib-sharp.dll ../lib/dotnet/taglib-sharp.dll
# extract downloaded zip files
unzip -uqo ../Downloads/ghostscript-win32.zip -d "../DistFiles/ghostscript"
unzip -uqo ../Downloads/Lame.zip -d "../lib/lame"
unzip -uqo ../Downloads/FirefoxDependencies.zip -d "../lib/FirefoxDependencies"
unzip -uqo ../Downloads/pdfjs-viewer.zip -d "../DistFiles/pdf"
# End of script
