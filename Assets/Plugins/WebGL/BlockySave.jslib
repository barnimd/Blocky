// Jembatan C# -> JavaScript untuk save di build Web.
// Dipanggil dari WebLocalStorageSaveStore (Assets/Scripts/Save/SaveStore.cs).
// try/catch: localStorage bisa diblokir browser (mode privat, cookie diblok).
mergeInto(LibraryManager.library, {
  BlockySave_GetInt: function (keyPtr, fallback) {
    try {
      var value = window.localStorage.getItem(UTF8ToString(keyPtr));
      if (value === null) {
        return fallback;
      }
      var parsed = parseInt(value, 10);
      return isNaN(parsed) ? fallback : parsed;
    } catch (e) {
      return fallback;
    }
  },

  BlockySave_SetInt: function (keyPtr, value) {
    try {
      window.localStorage.setItem(UTF8ToString(keyPtr), String(value));
    } catch (e) {
      console.warn("Blocky: gagal menyimpan progres", e);
    }
  },

  BlockySave_DeleteKey: function (keyPtr) {
    try {
      window.localStorage.removeItem(UTF8ToString(keyPtr));
    } catch (e) {
      console.warn("Blocky: gagal menghapus progres", e);
    }
  }
});
