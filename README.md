# EAPM — Euro & American Truck Simulator Profile & Save Manager

**EAPM** is a lightweight, modern utility designed to easily manage and edit save game profiles for *Euro Truck Simulator 2* and *American Truck Simulator*.

It allows you to:
- Copy existing profiles to new ones (cloning).
- Delete profiles securely.
- Backup profile directories directly to a zip file.
- View and edit profile statistics (Money, XP, and Driver Skills) for any save slot.

---

## ⚙️ How it Works & Requirements

### 1. Disable Steam Cloud Usage
Steam Cloud synchronization encrypts files in a way that prevents external editors from reading them. You must disable Steam Cloud usage for each profile you wish to edit:
1. Start the game.
2. In the profile selection screen, select your profile and click **Edit Profile**.
3. Uncheck the **Use Steam Cloud** checkbox and save.

### 2. Enable Decryptable Save Format
The game must be configured to write save files in a format that can be parsed:
1. Go to your documents directory:
   - **ETS2:** `Documents\Euro Truck Simulator 2`
   - **ATS:** `Documents\American Truck Simulator`
2. Open `config.cfg` in a text editor (like Notepad).
3. Search for: `uset g_save_format "0"`
4. Change it to: `uset g_save_format "2"`
5. Save the file, load your game, and save it once to apply the new format.

---

## 🛠️ Credits & Attribution

This project is built upon the work of several open-source tools and developers:

- **Origins & Evolution:** This project is a major, feature-rich evolution of the original *TruckSim-PM* by [elpatron68](https://github.com). While the original tool focused entirely on basic profile management, the core architecture, backend editing engine, and user interface have been **completely overhauled and rewritten from scratch** to support live save-game editing.
- **Decryption Engine:** Powered by [SII_Decrypt](https://github.com/TheLazyTomcat/SII_Decrypt) by *TheLazyTomcat* (Mozilla Public License 2.0).
- **UI Framework:** Styled using [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) (MIT License) and [Material Design Icons](https://github.com/MahApps/MahApps.Metro.IconPacks) (Apache License 2.0).
- **Exe Icon:** <a href="https://www.flaticon.com/free-icons/truck" title="truck icons">Truck icons created by Magnific - Flaticon</a>

---

### ✨ What's New in This Version:
* **Heavily Overhauled UI:** Completely modernized interface powered by MahApps.Metro and Material Design Icons.
* **Profile Renaming:** Dynamically changes your in-game profile name safely without corrupting files.
* **Save Editor Economy Suite:** Real-time editing for player XP/level, bank balance money, and unlocked driver skill points.
* **Retained Legacy Features:** Fast profile backups (ZIP format), deletion, and full manual `profile.sii` file decryption.

---

## 📄 License

This project is licensed under the **Apache License 2.0**. See the [LICENSE](LICENSE) file for details.

---

<p align="center">
  <a href="https://ko-fi.com/playhaux">
    <img src="https://img.shields.io/badge/Donate-Ko--fi-72a4f2?style=for-the-badge&logo=ko-fi&logoColor=white" alt="Donate on Ko-fi" />
  </a>
</p>

<p align="center">
  Designed with ❤️ by <a href="https://playhaux.com"><b>Playhaux</b></a>
</p>