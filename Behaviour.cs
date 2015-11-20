﻿using UnityEngine;
using System.IO;
using System;
using StatMaster.Data;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace StatMaster
{
    class Behaviour : MonoBehaviour
    {
        private GameData _data = new GameData();

        private uint eventsCallCount = 0;

        private uint tsSessionStart = 0;

        private bool validPark = true;

        private Settings _settings;

        private Notice _notice = new Notice();

        private IEnumerator _updateProgressionDataCoroutine;

        private void Awake()
        {
            Parkitect.UI.EventManager.Instance.OnStartPlayingPark += onStartPlayingParkHandler;

            StartCoroutine(loadSettingsProp());
        }

        void Start()
        {
            if (_settings.updateGameData)
            {
                Parkitect.UI.EventManager.Instance.OnStartPlayingPark -= onStartPlayingParkHandler;
                validPark = (GameController.Instance.park.guid != null);

                Debug.isEnable = _settings.devMode;

                initSession(false);
                if (validPark && _settings.updateParkData)
                {
                    GameController.Instance.park.OnNameChanged += onParkNameChangedHandler;
                    Parkitect.UI.EventManager.Instance.OnGameSaved += onGameSaved;

                    if (_settings.updateParkSessionData)
                    {
                        _updateProgressionDataCoroutine = updateProgressionData();
                        StartCoroutine(_updateProgressionDataCoroutine);
                    }
                }
                else if (!validPark)
                {
                    _notice.addText("The current park is not compatible with StatMaster!");
                    _notice.addText("No park data logging available, regular game data logging only!");
                    _notice.addText("We are working on a solution, please wait for the next pre-alpha 5 release!");
                    _notice.showWindow = true;

                    Debug.LogMT("No valid park found, park stats disabled!");
                }
                if (_data.oldDataVersion)
                {
                    _notice.addText("Detected old data version, old data files have been removed!");
                    _notice.addText("Main game data have been converted to new data version!");
                    _notice.addText("If you need old park / park session data have a look into:");
                    _notice.addText("persistentDataFolder/statMaster/backup_dv_num");
                    _notice.showWindow = true;
                    _data.oldDataVersion = false;

                    Debug.LogMT("Old data version detected!");
                }
            }
        }

        private IEnumerator loadSettingsProp()
        {
            while (_settings == null)
            {
                _settings = FindObjectOfType<Settings>();
                yield return new WaitForSeconds(1);
            }
        }

        private IEnumerator updateProgressionData()
        {
            while (true)
            {
                if (_settings.updateGameData &&
                    _settings.updateParkData && _settings.updateParkSessionData &&
                    _settings.updateProgressionData)
                {

                    uint cTs = Main.getCurrentTimestamp();

                    Debug.LogMT("Update progression data with interval " + _settings.progressionDataUpdateInterval);

                    // park progression count values
                    _data.currentPark.sessions[_data.currentPark.sessionIdx].progressionCountData.updateRange(
                        Convert.ToUInt32(GameController.Instance.park.getGuests().Count),
                        Convert.ToUInt32(GameController.Instance.park.getEmployees().Count),
                        Convert.ToUInt32(GameController.Instance.park.getAttractions().Count),
                        Convert.ToUInt32(GameController.Instance.park.getShops().Count)
                    );

                    // further park info values
                    _data.currentPark.sessions[_data.currentPark.sessionIdx].progressionFurtherData.updateRange(
                        GameController.Instance.park.parkInfo.money,
                        GameController.Instance.park.parkInfo.parkEntranceFee,
                        GameController.Instance.park.parkInfo.RatingCleanliness,
                        GameController.Instance.park.parkInfo.RatingHappiness,
                        GameController.Instance.park.parkInfo.RatingPriceSatisfaction
                    );

                    if (_settings.updatePeopleData)
                    {
                        Debug.LogMT("Update people progression data");

                        List<Person> people = GameController.Instance.park.people;
                        float moneyAvg = 0f, happinessAvg = 0f, tirednessAvg = 0f, hungerAvg = 0f,
                            thirstAvg = 0f, toiletUrgencyAvg = 0f, nauseaAvg = 0f;
                        for (int i = 0; i < people.Count; i++)
                        {
                            moneyAvg += people[i].Money;
                            happinessAvg += people[i].Happiness;
                            tirednessAvg += people[i].Tiredness;
                            hungerAvg += people[i].Hunger;
                            thirstAvg += people[i].Thirst;
                            toiletUrgencyAvg += people[i].ToiletUrgency;
                            nauseaAvg += people[i].Nausea;
                        }
                        if (moneyAvg > 0f)
                            moneyAvg = moneyAvg / people.Count;
                        if (happinessAvg > 0f)
                            happinessAvg = happinessAvg / people.Count;
                        if (tirednessAvg > 0f)
                            tirednessAvg = tirednessAvg / people.Count;
                        if (hungerAvg > 0f)
                            hungerAvg = hungerAvg / people.Count;
                        if (thirstAvg > 0f)
                            thirstAvg = thirstAvg / people.Count;
                        if (toiletUrgencyAvg > 0f)
                            toiletUrgencyAvg = toiletUrgencyAvg / people.Count;
                        if (nauseaAvg > 0f)
                            nauseaAvg = nauseaAvg / people.Count;

                        _data.currentPark.sessions[_data.currentPark.sessionIdx].peopleMoneyAvg.Add(
                          cTs, moneyAvg
                        );
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].peopleHappinessAvg.Add(
                          cTs, happinessAvg
                        );
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].peopleTirednessAvg.Add(
                          cTs, tirednessAvg
                        );
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].peopleHungerAvg.Add(
                          cTs, hungerAvg
                        );
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].peopleThirstAvg.Add(
                          cTs, thirstAvg
                        );
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].peopleToiletUrgencyAvg.Add(
                          cTs, toiletUrgencyAvg
                        );
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].peopleNauseaAvg.Add(
                          cTs, nauseaAvg
                        );
                    }

                    // attractions values
                    if (_settings.updateAttractionsData)
                    {
                        Debug.LogMT("Update attractions progression data");

                        float attractionsEntranceFeeAvg = 0f;
                        ReadOnlyCollection<Attraction> attractions = GameController.Instance.park.getAttractions();
                        for (int i = 0; i < attractions.Count; i++)
                        {
                            attractionsEntranceFeeAvg += attractions[i].entranceFee;
                        }
                        if (attractionsEntranceFeeAvg > 0f)
                            attractionsEntranceFeeAvg = attractionsEntranceFeeAvg / attractions.Count;
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].attractionsEntranceFeeAvg.Add(
                          cTs, attractionsEntranceFeeAvg
                        );

                        // todo: improvements to get correct relations 
                        // to changes in attractions builded / destroyed status progression
                        // related events available? need more info
                        uint attractionsOpenedCount = 0;
                        uint attractionsCustomersCount = 0;
                        for (int i = 0; i < attractions.Count; i++)
                        {
                            if (attractions[i].state == Attraction.State.OPENED) attractionsOpenedCount++;
                            attractionsCustomersCount = Convert.ToUInt32(attractions[i].customersCount);
                        }
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].attractionsOpenedCount.Add(
                            cTs, attractionsOpenedCount
                        );
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].attractionsCustomersCount.Add(
                            cTs, attractionsCustomersCount
                        );
                    }

                    if (_settings.updateShopsData)
                    {
                        Debug.LogMT("Update shops progression data");

                        float shopsProductPriceAvg = 0f;
                        ReadOnlyCollection<Shop> shops = GameController.Instance.park.getShops();
                        ProductShop ps;
                        ProductShopSettings pss;
                        float productPriceAvg;
                        for (int i = 0; i < shops.Count; i++) {
                            ps = (ProductShop)shops[i];
                            pss = (ProductShopSettings)ps.getSettings();
                            productPriceAvg = 0f;
                            for (int j = 0; j < ps.products.Length; j++)
                            {
                                productPriceAvg += pss.getProductSettings(ps.products[j]).price;
                            }
                            if (productPriceAvg > 0f)
                                productPriceAvg = productPriceAvg / ps.products.Length;
                            shopsProductPriceAvg += productPriceAvg;
                        }
                        if (shopsProductPriceAvg > 0f)
                            shopsProductPriceAvg = shopsProductPriceAvg / shops.Count;
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].shopsProductPriceAvg.Add(
                          cTs, shopsProductPriceAvg
                        );

                        // todo: improvements to get correct relations 
                        // to changes in shops builded / destroyed status progression
                        // related events available? need more info
                        uint shopsOpenedCount = 0;
                        uint shopsCustomersCount = 0;
                        for (int i = 0; i < shops.Count; i++)
                        {
                            if (shops[i].opened) shopsOpenedCount++;
                            shopsCustomersCount = Convert.ToUInt32(shops[i].customersCount);
                        }
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].shopsOpenedCount.Add(
                            cTs, shopsOpenedCount
                        );
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].shopsCustomersCount.Add(
                            cTs, shopsCustomersCount
                        );
                    }
                }

                yield return new WaitForSeconds(_settings.progressionDataUpdateInterval);
            }
        }

        private void onGameSaved()
        {
            Debug.LogMT("Game saved");
            addParkFile("save");

            if (_settings.updateAutoSaveData)
                addAutoSaveParkFileToData(getParkFileName(), "save");

            eventsCallCount++;
        }

        private void onParkNameChangedHandler(string oldName, string newName)
        {
            Debug.LogMT("Park name change to " + newName);
            addParkName(newName);
            eventsCallCount++;
        }

        private void onStartPlayingParkHandler()
        {
            tsSessionStart = Main.getCurrentTimestamp();
            _data.currentGameStart = tsSessionStart;
            Debug.LogMT("Started playing at ", tsSessionStart);
            eventsCallCount++;
        }

        private void initSession(bool resetGameData) {
            Debug.LogMT("Init session");
            if (tsSessionStart == 0) tsSessionStart = Main.getCurrentTimestamp();
            if (resetGameData) _data = new GameData();
            _data.currentGameStart = tsSessionStart;
            if (validPark) _data.currentParkGuid = GameController.Instance.park.guid.ToLower();
            Debug.LogMT(_data.loadByHandles());

            _data.init();

            Debug.LogMT("New data? " + !(_data.sessionIdx > 0));
            
            Debug.LogMT("Current session start time ", 
                _data.tsSessionStarts[_data.tsSessionStarts.Count - 1]);

            updateData("load");
        }

        private bool addParkFileToData(string name, string mode = "load")
        {
            bool success = false;
            string cName = (_data.currentPark.files.Count > 0) 
                ? _data.currentPark.files[_data.currentPark.files.Count - 1] : null;
            if (cName != name)
            {
                _data.currentPark.files.Add(name);
                if (_settings.updateParkSessionData)
                {
                    if (mode == "save")
                    {
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].saveFiles.Add(name);
                    }
                    else
                    {
                        _data.currentPark.sessions[_data.currentPark.sessionIdx].loadFile = name;
                    }
                }
                success = true;
            }
            return success;
        }

        private bool addAutoSaveParkFileToData(string name, string mode = "load")
        {
            bool success = false;
            if (name.IndexOf("QuickSave-") != -1 || name.IndexOf("AutoSave-") != -1)
            {
                bool isAutoSave = (name.IndexOf("AutoSave-") != -1);
                Debug.LogMT("AutoSave file to data " + name);

                if (isAutoSave) _data.currentPark.autoSavesCount++;
                if (!isAutoSave) _data.currentPark.quickSavesCount++;

                if (_settings.updateParkSessionData)
                {
                    if (isAutoSave) _data.currentPark.sessions[_data.currentPark.sessionIdx].autoSavesCount++;
                    if (!isAutoSave) _data.currentPark.sessions[_data.currentPark.sessionIdx].quickSavesCount++;
                }
                success = true;
            }
            
            return success;
        }

        private string getParkFileName()
        {
            string[] parkSaveFileElements = GameController.Instance.loadedSavegamePath.Split(
                    (Application.platform == RuntimePlatform.WindowsPlayer) ? '\\' : '/'
                );
            return parkSaveFileElements[parkSaveFileElements.Length - 1];
        }

        private void addParkFile(string mode = "load")
        {
            bool updated = false;
            Debug.LogMT("Add park file");
            if (File.Exists(GameController.Instance.loadedSavegamePath))
            {
                string dFile = getParkFileName();

                if ((!_settings.ignoreQuickSaveFileNames ||
                     (_settings.ignoreQuickSaveFileNames && dFile.IndexOf("QuickSave-") == -1)) &&
                    (!_settings.ignoreAutoSaveFileNames ||
                     (_settings.ignoreAutoSaveFileNames && dFile.IndexOf("AutoSave-") == -1)))
                {
                    updated = addParkFileToData(dFile, mode);
                    if (updated) Debug.LogMT("New park file " + dFile + " mode (" + mode + ")");
                }
            }
            if (updated == false) Debug.LogMT("No new park file");
        }

        private bool addParkName(string name)
        {
            ParkData pd = _data.currentPark;
            bool success = false;
            if (name != "Unnamed Park" && 
                (pd.names.Count == 0 || pd.names[pd.names.Count - 1] != name))
            {
                Debug.LogMT("New park name " + name);
                pd.names.Add(name);
                if (_settings.updateParkSessionData)
                    pd.sessions[pd.sessionIdx].names.Add(name);
                success = true;
            }
            else
            {
                Debug.LogMT("No new park name");
            }
            return success;
        }

        private void updateData(string mode = "load")
        {
            Debug.LogMT("Update game data");

            uint cTs = Main.getCurrentTimestamp();
            _data.tsEnd = cTs;

            if (validPark && _settings.updateParkData)
            {
                Debug.LogMT("Update park data with session " + _data.currentPark.sessionIdx);

                _data.currentPark.tsEnd = cTs;
                Debug.LogMT("Current session end time ", _data.tsEnd);

                Debug.LogMT("New park time " + ParkInfo.ParkTime.ToString());
                _data.currentPark.time = Convert.ToUInt32(ParkInfo.ParkTime);

                if (_settings.updateParkSessionData)
                {
                    _data.currentPark.sessions[_data.currentPark.sessionIdx].update(
                        _data.currentPark.time
                    );
                }

                string parkName = GameController.Instance.park.parkName;
                addParkName(parkName);
                addParkFile(mode);
           }
        }

        private void OnGUI()
        {
            if (Application.loadedLevel != 2 || _settings.devMode != true)
                return;

            _notice.OnGUI();

            if (GUI.Button(new Rect(Screen.width - 200, 0, 200, 20), "Save Data"))
            {
                updateData("save");
                Debug.LogMT(_data.saveByHandles());
            }
            if (GUI.Button(new Rect(Screen.width - 200, 30, 200, 20), "Next Session"))
            {
                updateData("save");
                Debug.LogMT(_data.saveByHandles());
                tsSessionStart = 0;
                initSession(true);
            }
            if (GUI.Button(new Rect(Screen.width - 200, 60, 200, 20), "Reset Data"))
            {
                FilesHandler fh = new FilesHandler();
                fh.deleteAll();
                Debug.LogMT("All data files have been deleted");
                tsSessionStart = 0;
                initSession(true);
            }
            if (GUI.Button(new Rect(Screen.width - 200, 90, 200, 20), "Debug Data"))
            {
                updateData("save");
                Debug.LogMT("Current session data");
                Debug.LogMT("Events proceed " + eventsCallCount.ToString());
                string[] names = { "gameTime", "sessionTime" };
                long[] values = {
                    Convert.ToInt64(_data.tsEnd - _data.tsStart) * 1000,
                    Convert.ToInt64(_data.tsEnd - _data.tsSessionStarts[_data.sessionIdx]) * 1000
                };
                Debug.LogMT(names, values, 2);

            }
        }

        void OnDisable()
        {
            if (_settings.updateGameData) { 
                if (validPark && _settings.updateParkData)
                {
                    if (_settings.updateParkSessionData)
                        StopCoroutine(_updateProgressionDataCoroutine);

                    GameController.Instance.park.OnNameChanged -= onParkNameChangedHandler;
                    Parkitect.UI.EventManager.Instance.OnGameSaved -= onGameSaved;
                }
                updateData("save");
                _data.saveByHandles();
            }
        }

    }
}
