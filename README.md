# mapnames

Simple tray app to monitor changes to a quake .map file. 
Any non brush entity is checked and appended a "unityname" class, and "namevalue", where namevalue is based on the entity's class and an integer.
These names are used for importing to unity to keep unique references to each entity between changes.

Worldspawn and func_group are ignored.
