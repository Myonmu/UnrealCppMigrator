# Unreal Cpp Migrator

This little program batch-processes macro replacement and core redirect additions after moving c++ files from one module to another.

For example, if you move class `MyClass` from `ModuleA` to `ModuleB`, the program detects this refactoring and will replace `MODULEA_API` with `MODULEB_API`, while adding
```
+ClassRedirect=(OldName="/Script/ModuleA.MyClass",NewName"Script/ModuleB.MyClass")
```
to your `DefaultEngine.ini` under the `[CoreRedirects]` section (well, it actually creates a new Core Redirects section).

It expects the default source structue, and the program should either sit at your project root or you provide project root as command line argument. (The exe file should be at the same level of your **Source** and **Config** folders if you don't pass command line arguments). 

## Limitations

### Folder Structure

As stated before, your code should follow the default source folders structure, that is :

```
---ProjectRoot
   |-Source
   |   |-ModuleA
   |   |  |-Public
   |   |  |  |-MyClass1.h
   |   |  |
   |   |  |-Private
   |   |  |  |-MyClass1.cpp
   |   |      
   |   |-ModuleB
   |   |  |-Public
   |   |  |  |-MyClass2.h
   |   |  |
   |   |  |-Private
   |   |  |  |-MyClass2.cpp
```

The program currently does not detect any sub folders under Public or Private folders even though they are perfectly legal.

When moving, it expects moved files to respect the same relative path to module root as in its origin module.

### Ini file cleanup

It does not clean-up ini file for non-existent classes or multiple redirects that can be merged. 

## Under the Hood

When moving source files, especially the header files that inherit from UObject, the API macro is not changed, and will cause build errors. We can figure out the correct macro by following the steps:

1. Find out the module name that the header currently resides in. This is simply the module's root folder name.
2. Make the module name ALLCAPS
3. Append with `_API`

This process is bijective, so from the name of the macro we can backtrack to find the name of the module. Therefore, a header file with erroneous api macro allows us to figure out which module the header file comes from and the correct macro. 
