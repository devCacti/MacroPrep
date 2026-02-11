window.MacroPrep_DB = {
    dbName: 'MacroPrep_DB',
    version: 2,

    // Helper to open the DB
    openDb: function () {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.version);

            request.onupgradeneeded = (event) => {
                const db = event.target.result;

                // Create Lists Store
                if (!db.objectStoreNames.contains('lists')) {
                    db.createObjectStore('lists', { keyPath: 'id' });
                }

                // Create Items Store with Index for querying by ListId
                if (!db.objectStoreNames.contains('items')) {
                    const itemsStore = db.createObjectStore('items', { keyPath: 'id' });
                    itemsStore.createIndex('listId', 'listId', { unique: false });
                }

                // Create Members Store
                if (!db.objectStoreNames.contains('members')) {
                    db.createObjectStore('members', { keyPath: 'id' });
                }
            };

            request.onsuccess = (event) => resolve(event.target.result);
            request.onerror = (event) => reject(event.target.error);
        });
    },

    // 1. GET ALL
    getAll: function (storeName) {
        return new Promise((resolve, reject) => {
            this.openDb().then(db => {
                const tx = db.transaction(storeName, 'readonly');
                const store = tx.objectStore(storeName);
                const request = store.getAll();

                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
        });
    },

    // 2. GET BY ID
    get: function (storeName, id) {
        return new Promise((resolve, reject) => {
            this.openDb().then(db => {
                const tx = db.transaction(storeName, 'readonly');
                const store = tx.objectStore(storeName);
                const request = store.get(id);

                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
        });
    },

    // 3. SAVE (Add/Update)
    save: function (storeName, item) {
        return new Promise((resolve, reject) => {
            this.openDb().then(db => {
                const tx = db.transaction(storeName, 'readwrite');
                const store = tx.objectStore(storeName);
                const request = store.put(item); // 'put' updates if key exists, adds if not

                request.onsuccess = () => resolve();
                request.onerror = () => reject(request.error);
            });
        });
    },

    // 4. DELETE
    delete: function (storeName, id) {
        return new Promise((resolve, reject) => {
            this.openDb().then(db => {
                const tx = db.transaction(storeName, 'readwrite');
                const store = tx.objectStore(storeName);
                const request = store.delete(id);

                request.onsuccess = () => resolve();
                request.onerror = () => reject(request.error);
            });
        });
    },

    // 5. GET ITEMS BY LIST ID (Using Index)
    getItemsByList: function (listId) {
        return new Promise((resolve, reject) => {
            this.openDb().then(db => {
                const tx = db.transaction('items', 'readonly');
                const store = tx.objectStore('items');
                const index = store.index('listId');
                const request = index.getAll(listId);

                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
        });
    },

    getMembersByList: function (listId) {
        return new Promise((resolve, reject) => {
            this.openDb().then(db => {
                const tx = db.transaction('members', 'readonly');
                const store = tx.objectStore('members');
                const request = store.getAll(); // No index, so get all and filter

                request.onsuccess = () => {
                    const allMembers = request.result;
                    const filtered = allMembers.filter(m => m.listId === listId);
                    resolve(filtered);
                };
                request.onerror = () => reject(request.error);
            });
        });
    },

    getMemberByListAndUserName: function (listId, username) {
        return new Promise((resolve, reject) => {
            this.openDb().then(db => {
                const tx = db.transaction('members', 'readonly');
                const store = tx.objectStore('members');
                const request = store.getAll(); // No index, so get all and filter

                request.onsuccess = () => {
                    const allMembers = request.result;
                    const member = allMembers.find(m => m.listId === listId && m.userName === username);
                    resolve(member);
                };
                request.onerror = () => reject(request.error);
            });
        });
    },

    // 6. MARK SYNCHRONIZED
    markSynced: function (storeName, id) {
        return new Promise((resolve, reject) => {
            this.openDb().then(db => {
                const tx = db.transaction(storeName, 'readwrite');
                const store = tx.objectStore(storeName);

                // Get the item first
                const getReq = store.get(id);

                getReq.onsuccess = () => {
                    const item = getReq.result;
                    if (item) {
                        if (item.isDeleted) {
                            // If it was a soft delete and we synced it, hard delete now
                            store.delete(id);
                        } else {
                            // Mark clean
                            item.isSynced = true;
                            store.put(item);
                        }
                    }
                    resolve();
                };
                getReq.onerror = () => reject(getReq.error);
            });
        });
    },

    // Init function (wrapper to ensure DB is created)
    init: function () {
        return this.openDb();
    }
};