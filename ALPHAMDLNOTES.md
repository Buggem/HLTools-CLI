# Other model textures

## Version 4 (0.52 desert, olive grunts)
I could find very little information on this. 

**What I found so far:**
 - NOT a Quake file format (Quake uses IDPO for ID polygon, this uses modern magic IDST)
 - seems somewhat simillar to Quake file format, and very far from modern GoldSrc format
 - no documentation, anywhere on the internet.
 - only people who extracted it successfully are TCRF people (staying away bc freaky)

**Ideas for the future:**
 - Check out HLAM, loads of useful things there (wonder why they archived it)
 - Check the Alpha executables for symbols in Ghidra and decompile

## Version 6 (main 0.52 ver)
Implemented, seek to index 100 (0x64) for numTextures, textureIndex, textureDataIndex in specific case.
TODO: investigation into structure just for fun
