#!/bin/bash

if [ "$1" = "--help"  ] || [ "$1" = "-help" ] || [ "$1" = "-h" ]
then
    printf "%s\n" "Usage: $0 [-help/-lib/-dnet/clean]"
    printf "%s\n%s\n%s\n%s\n" \
        "  -help: shows this message" \
        "  -lib: compiles without CLI as a library" \
        "  -dnet: [UNSUPPORTED] try .NET framework build" "" \
        "   clean: clean up the build dir"
    exit 0
fi

if [ "$1" = "clean" ]
then
    rm -rf obj # only made if building through .NET instead of mono, which sucks anyway
    rm -rf bin
    exit 0
fi

# at this point we can safely make the binary out dir assuming a compilation is taking place
mkdir -p bin

if [ "$1" = "-dnet" ]
then
    dotnet build
    exit 0
fi

# brand new thing
MCSOPT="-out:bin/HLTools.exe"
if [ "$1" = "-lib" ]
then
    MCSOPT="-target:library -out:bin/HLTools.dll"
fi

if [ "$1" = "-debug" ]
then
    MCSOPT="$MCSOPT -debug"
fi


mcs $MCSOPT \
    -reference:lib/FreeImageNET.dll \
    -reference:System.Drawing.dll \
    Main.cs TextureExceptions.cs SpriteLoader.cs WAD3Loader.cs MDLLoader.cs CLIHelper.cs

# copy library to bin folder
cp lib/FreeImageNET.dll bin/
