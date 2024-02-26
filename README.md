# Unreal Cpp Migrator

This little program batch-processes macro replacement and core redirect additions after moving c++ files from one module to another.

For example, if you move class `MyClass` from `ModuleA` to `ModuleB`, the program detects this refactoring and will replace `MODULEA_API` with `MODULEB_API`, while adding
```
+ClassRedirect=(OldName="/Script/ModuleA.MyClass",NewName"Script/ModuleB.MyClass")
```
to your `DefaultEngine.ini` under the `[CoreRedirects]` section (well, it actually creates a new Core Redirects section).

It expects the default source structue, and the program should either sit at your project root or you provide project root as command line argument.
