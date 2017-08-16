#!/bin/bash
# server=build.palaso.org
# project=Bloom
# build=Bloom-Default-Linux64-Continuous
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
copy_curl $1 $2
elif [ "$where_wget" != "" ]
then
copy_wget $1 $2
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
curl -# -L -z $2 -o $2 $1
else
curl -# -L -o $2 $1
fi
}

copy_wget() {
echo "wget: $2 <= $1"
f1=$(basename $1)
f2=$(basename $2)
cd $(dirname $2)
wget -q -L -N $1
# wget has no true equivalent of curl's -o option.
# Different versions of wget handle (or not) % escaping differently.
# A URL query is the only reason why $f1 and $f2 should differ.
if [ "$f1" != "$f2" ]; then mv $f2\?* $f2; fi
cd -
}


# *** Results ***
# build: Bloom-Default-Linux64-Continuous (bt403)
# project: Bloom
# URL: http://build.palaso.org/viewType.html?buildTypeId=bt403
# VCS: git://github.com/BloomBooks/BloomDesktop.git [master]
# dependencies:
# [0] build: bloom-win32-static-dependencies (bt396)
#     project: Bloom
#     URL: http://build.palaso.org/viewType.html?buildTypeId=bt396
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"connections.dll"=>"DistFiles", "MSBuild.Community.Tasks.dll"=>"build/", "MSBuild.Community.Tasks.Targets"=>"build/"}
# [1] build: BloomPlayer-Master-Continuous (BPContinuous)
#     project: Bloom
#     URL: http://build.palaso.org/viewType.html?buildTypeId=BPContinuous
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"*.*"=>"DistFiles/"}
#     VCS: https://github.com/BloomBooks/BloomPlayer [refs/heads/master]
# [2] build: YouTrackSharp (Bloom_YouTrackSharp)
#     project: Bloom
#     URL: http://build.palaso.org/viewType.html?buildTypeId=Bloom_YouTrackSharp
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"bin/YouTrackSharp.dll"=>"lib/dotnet", "bin/YouTrackSharp.pdb"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/YouTrackSharp.git [LinuxCompatible]
# [3] build: Bloom Help 4.0 (Bloom_Help_BloomHelp40)
#     project: Help
#     URL: http://build.palaso.org/viewType.html?buildTypeId=Bloom_Help_BloomHelp40
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"*.chm"=>"DistFiles"}
# [4] build: pdf.js (bt401)
#     project: BuildTasks
#     URL: http://build.palaso.org/viewType.html?buildTypeId=bt401
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"pdfjs-viewer.zip!**"=>"DistFiles/pdf"}
#     VCS: https://github.com/mozilla/pdf.js.git [gh-pages]
# [5] build: GeckofxHtmlToPdf-xenial64-continuous (GeckofxHtmlToPdf_GeckofxHtmlToPdfXenial64continuous)
#     project: GeckofxHtmlToPdf
#     URL: http://build.palaso.org/viewType.html?buildTypeId=GeckofxHtmlToPdf_GeckofxHtmlToPdfXenial64continuous
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"Args.dll"=>"lib/dotnet", "GeckofxHtmlToPdf.exe"=>"lib/dotnet", "GeckofxHtmlToPdf.exe.config"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/geckofxHtmlToPdf [refs/heads/master]
# [6] build: L10NSharp xliff Mono continuous (L10NSharpXliffMonoContinuous)
#     project: L10NSharp
#     URL: http://build.palaso.org/viewType.html?buildTypeId=L10NSharpXliffMonoContinuous
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"L10NSharp.dll*"=>"lib/dotnet/"}
#     VCS: https://github.com/sillsdev/l10nsharp [xliff]
# [7] build: icucil-linux64-Continuous (bt281)
#     project: Libraries
#     URL: http://build.palaso.org/viewType.html?buildTypeId=bt281
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"icu.net.*"=>"lib/dotnet/icu48"}
#     VCS: https://github.com/sillsdev/icu-dotnet [master]
# [8] build: icucil-linux64-icu55 Continuous (Icu55)
#     project: Libraries
#     URL: http://build.palaso.org/viewType.html?buildTypeId=Icu55
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"icu.net.*"=>"lib/dotnet/icu55"}
#     VCS: https://github.com/sillsdev/icu-dotnet [master]
# [9] build: icucil-precise64-icu52 Continuous (bt413)
#     project: Archived
#     URL: http://build.palaso.org/viewType.html?buildTypeId=bt413
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"icu.net.*"=>"lib/dotnet/icu52"}
#     VCS: https://github.com/sillsdev/icu-dotnet [master]
# [10] build: PdfDroplet-Linux-Dev-Continuous (bt344)
#     project: PdfDroplet
#     URL: http://build.palaso.org/viewType.html?buildTypeId=bt344
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"PdfDroplet.exe"=>"lib/dotnet", "PdfSharp.dll*"=>"lib/dotnet"}
#     VCS: https://github.com/sillsdev/pdfDroplet [master]
# [11] build: TidyManaged-master-linux64-continuous (bt351)
#     project: TidyManaged
#     URL: http://build.palaso.org/viewType.html?buildTypeId=bt351
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"TidyManaged.dll*"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/TidyManaged.git [master]
# [12] build: Linux master continuous (XliffForHtml_LinuxMasterContinuous)
#     project: XliffForHtml
#     URL: http://build.palaso.org/viewType.html?buildTypeId=XliffForHtml_LinuxMasterContinuous
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"HtmlXliff.*"=>"lib/dotnet"}
#     VCS: https://github.com/sillsdev/XliffForHtml [refs/heads/master]
# [13] build: palaso-linux64-master Continuous (Libpalaso_PalasoLinux64masterContinuous)
#     project: libpalaso
#     URL: http://build.palaso.org/viewType.html?buildTypeId=Libpalaso_PalasoLinux64masterContinuous
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"SIL.BuildTasks.dll"=>"build/", "Newtonsoft.Json.dll"=>"lib/dotnet/", "SIL.Core.dll*"=>"lib/dotnet/", "SIL.Core.Desktop.dll*"=>"lib/dotnet/", "SIL.Media.dll*"=>"lib/dotnet/", "SIL.TestUtilities.dll*"=>"lib/dotnet/", "SIL.Windows.Forms.dll*"=>"lib/dotnet/", "SIL.Windows.Forms.GeckoBrowserAdapter.dll*"=>"lib/dotnet/", "SIL.Windows.Forms.Keyboarding.dll*"=>"lib/dotnet/", "SIL.Windows.Forms.WritingSystems.dll*"=>"lib/dotnet/", "SIL.WritingSystems.dll*"=>"lib/dotnet/", "taglib-sharp.dll*"=>"lib/dotnet/"}
#     VCS: https://github.com/sillsdev/libpalaso.git [master]

# make sure output directories exist
mkdir -p ../DistFiles
mkdir -p ../DistFiles/
mkdir -p ../DistFiles/pdf
mkdir -p ../Downloads
mkdir -p ../build/
mkdir -p ../lib/dotnet
mkdir -p ../lib/dotnet/
mkdir -p ../lib/dotnet/icu48
mkdir -p ../lib/dotnet/icu52
mkdir -p ../lib/dotnet/icu55

# download artifact dependencies
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/latest.lastSuccessful/connections.dll ../DistFiles/connections.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/latest.lastSuccessful/MSBuild.Community.Tasks.dll ../build/MSBuild.Community.Tasks.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt396/latest.lastSuccessful/MSBuild.Community.Tasks.Targets ../build/MSBuild.Community.Tasks.Targets
copy_auto http://build.palaso.org/guestAuth/repository/download/BPContinuous/latest.lastSuccessful/bloomPlayer.js ../DistFiles/bloomPlayer.js
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_YouTrackSharp/latest.lastSuccessful/bin/YouTrackSharp.dll ../lib/dotnet/YouTrackSharp.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_YouTrackSharp/latest.lastSuccessful/bin/YouTrackSharp.pdb ../lib/dotnet/YouTrackSharp.pdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Bloom_Help_BloomHelp40/latest.lastSuccessful/Bloom.chm ../DistFiles/Bloom.chm
copy_auto http://build.palaso.org/guestAuth/repository/download/bt401/latest.lastSuccessful/pdfjs-viewer.zip ../Downloads/pdfjs-viewer.zip
copy_auto http://build.palaso.org/guestAuth/repository/download/GeckofxHtmlToPdf_GeckofxHtmlToPdfXenial64continuous/latest.lastSuccessful/Args.dll ../lib/dotnet/Args.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/GeckofxHtmlToPdf_GeckofxHtmlToPdfXenial64continuous/latest.lastSuccessful/GeckofxHtmlToPdf.exe ../lib/dotnet/GeckofxHtmlToPdf.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/GeckofxHtmlToPdf_GeckofxHtmlToPdfXenial64continuous/latest.lastSuccessful/GeckofxHtmlToPdf.exe.config ../lib/dotnet/GeckofxHtmlToPdf.exe.config
copy_auto http://build.palaso.org/guestAuth/repository/download/L10NSharpXliffMonoContinuous/latest.lastSuccessful/L10NSharp.dll ../lib/dotnet/L10NSharp.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/L10NSharpXliffMonoContinuous/latest.lastSuccessful/L10NSharp.dll.mdb ../lib/dotnet/L10NSharp.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/bt281/latest.lastSuccessful/icu.net.0.0.0.0.nupkg ../lib/dotnet/icu48/icu.net.0.0.0.0.nupkg
copy_auto http://build.palaso.org/guestAuth/repository/download/bt281/latest.lastSuccessful/icu.net.dll ../lib/dotnet/icu48/icu.net.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt281/latest.lastSuccessful/icu.net.dll.config ../lib/dotnet/icu48/icu.net.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/bt281/latest.lastSuccessful/icu.net.dll.mdb ../lib/dotnet/icu48/icu.net.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Icu55/latest.lastSuccessful/icu.net.0.0.0.0.nupkg ../lib/dotnet/icu55/icu.net.0.0.0.0.nupkg
copy_auto http://build.palaso.org/guestAuth/repository/download/Icu55/latest.lastSuccessful/icu.net.dll ../lib/dotnet/icu55/icu.net.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Icu55/latest.lastSuccessful/icu.net.dll.config ../lib/dotnet/icu55/icu.net.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/Icu55/latest.lastSuccessful/icu.net.dll.mdb ../lib/dotnet/icu55/icu.net.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/bt413/latest.lastSuccessful/icu.net.dll ../lib/dotnet/icu52/icu.net.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt413/latest.lastSuccessful/icu.net.dll.config ../lib/dotnet/icu52/icu.net.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/bt413/latest.lastSuccessful/icu.net.dll.mdb ../lib/dotnet/icu52/icu.net.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/bt344/latest.lastSuccessful/PdfDroplet.exe ../lib/dotnet/PdfDroplet.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/bt344/latest.lastSuccessful/PdfSharp.dll ../lib/dotnet/PdfSharp.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt351/latest.lastSuccessful/TidyManaged.dll ../lib/dotnet/TidyManaged.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt351/latest.lastSuccessful/TidyManaged.dll.config ../lib/dotnet/TidyManaged.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/XliffForHtml_LinuxMasterContinuous/latest.lastSuccessful/HtmlXliff.exe ../lib/dotnet/HtmlXliff.exe
copy_auto http://build.palaso.org/guestAuth/repository/download/XliffForHtml_LinuxMasterContinuous/latest.lastSuccessful/HtmlXliff.exe.mdb ../lib/dotnet/HtmlXliff.exe.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.BuildTasks.dll ../build/SIL.BuildTasks.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/Newtonsoft.Json.dll ../lib/dotnet/Newtonsoft.Json.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Core.dll ../lib/dotnet/SIL.Core.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Core.dll.config ../lib/dotnet/SIL.Core.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Core.dll.mdb ../lib/dotnet/SIL.Core.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Core.Desktop.dll ../lib/dotnet/SIL.Core.Desktop.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Core.Desktop.dll.mdb ../lib/dotnet/SIL.Core.Desktop.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Media.dll ../lib/dotnet/SIL.Media.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Media.dll.config ../lib/dotnet/SIL.Media.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Media.dll.mdb ../lib/dotnet/SIL.Media.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.TestUtilities.dll ../lib/dotnet/SIL.TestUtilities.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.TestUtilities.dll.mdb ../lib/dotnet/SIL.TestUtilities.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.dll ../lib/dotnet/SIL.Windows.Forms.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.dll.config ../lib/dotnet/SIL.Windows.Forms.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.dll.mdb ../lib/dotnet/SIL.Windows.Forms.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.GeckoBrowserAdapter.dll ../lib/dotnet/SIL.Windows.Forms.GeckoBrowserAdapter.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.GeckoBrowserAdapter.dll.mdb ../lib/dotnet/SIL.Windows.Forms.GeckoBrowserAdapter.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.Keyboarding.dll ../lib/dotnet/SIL.Windows.Forms.Keyboarding.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.Keyboarding.dll.config ../lib/dotnet/SIL.Windows.Forms.Keyboarding.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.Keyboarding.dll.mdb ../lib/dotnet/SIL.Windows.Forms.Keyboarding.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.WritingSystems.dll ../lib/dotnet/SIL.Windows.Forms.WritingSystems.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.Windows.Forms.WritingSystems.dll.mdb ../lib/dotnet/SIL.Windows.Forms.WritingSystems.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.WritingSystems.dll ../lib/dotnet/SIL.WritingSystems.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/SIL.WritingSystems.dll.mdb ../lib/dotnet/SIL.WritingSystems.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/Libpalaso_PalasoLinux64masterContinuous/latest.lastSuccessful/taglib-sharp.dll ../lib/dotnet/taglib-sharp.dll
# extract downloaded zip files
unzip -uqo ../Downloads/pdfjs-viewer.zip -d ../DistFiles/pdf
# End of script
