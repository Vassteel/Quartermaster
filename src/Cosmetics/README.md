# Gull visual source

These original sources are vendored from the adjacent Valheim Helmsman project so Quartermaster needs no Helmsman or Jötunn runtime dependency. GullHelmet.cs is byte-identical in both projects; GullMeshRig and GullPerformance use the Quartermaster.Cosmetics namespace and the Quartermaster logger. Runtime bird meshes and textures are borrowed from the installed vanilla Seagal prefab, never shipped in either package.

When changing the helmet, keep both copies aligned. The readable sitting mesh uses measured vanilla head/neck/body coordinates; a private clone is deformed and destroyed with the actor.
