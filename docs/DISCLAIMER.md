# ScrcpyDeX — Legal Disclaimer & Terms of Interoperability

**Project:** ScrcpyDeX  
**Primary Author & Copyright Holder:** Aureliano Peixoto and ScrcpyDeX Contributors  
**Governing License:** Apache License, Version 2.0  
**Effective Date:** 2026  

---

## 1. General Disclaimer of Warranty & Limitation of Liability

### 1.1 "AS IS" Provision
THE SOFTWARE "SCRCPYDEX" (INCLUDING ALL ASSOCIATED SCRIPTS, BINARIES, COMPILED BYTECODE, DOCUMENTATION, AND GRAPHICAL INTERFACES) IS PROVIDED ON AN **"AS IS"** AND **"AS AVAILABLE"** BASIS, WITH ALL FAULTS AND WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS, IMPLIED, STATUTORY, OR OTHERWISE.

### 1.2 Comprehensive Disclaimer of Warranties
TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, THE AUTHOR(S), MAINTAINER(S), AND CONTRIBUTORS EXPRESSLY DISCLAIM ALL WARRANTIES OF ANY KIND, WHETHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO:
1. **MERCHANTABILITY:** THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE OR REQUIREMENT.
2. **NON-INFRINGEMENT:** TITLE, SECURITY, WORKMANLIKE EFFORT, ACCURACY, AND NON-INFRINGEMENT OF THIRD-PARTY INTELLECTUAL PROPERTY RIGHTS.
3. **UNINTERRUPTED OPERATION:** FREEDOM FROM PROGRAM ERRORS, DEFECTS, GLITCHES, LATENCY, PERFORMANCE ANOMALIES, INTERRUPTIONS, PROTOCOL DISRUPTIONS, OR HARDWARE/SOFTWARE INCOMPATIBILITIES.
4. **DATA PRESERVATION:** ABSOLUTE SAFETY AGAINST DATA CORRUPTION, OPERATIONAL DOWNTIME, UNEXPECTED TELEMETRY LOGGING, OR SYSTEM STATE CHANGES.

### 1.3 Limitation of Liability
IN NO EVENT SHALL THE PRIMARY AUTHOR (AURELIANO PEIXOTO), ANY PROJECT MAINTAINER, OR CONTRIBUTOR BE LIABLE UNDER ANY LEGAL THEORY—WHETHER IN CONTRACT, TORT (INCLUDING NEGLIGENCE), STRICT LIABILITY, INDEMNITY, PRODUCT LIABILITY, OR OTHERWISE—FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, PUNITIVE, OR CONSEQUENTIAL DAMAGES ARISING OUT OF OR IN CONNECTION WITH:
* THE USE, MISUSE, INABILITY TO USE, OR MALFUNCTION OF THE SOFTWARE;
* ANY PHYSICAL DAMAGE, OVERHEATING, BATTERY WEAR, HARDWARE FAILURE, SYSTEM BRICKING, OR BOOTLOOP INCURRED ON THE TARGET DEVICE OR THE HOST PC;
* ANY LOSS OF BUSINESS PROFITS, REVENUE, GOODWILL, REPUTATION, CONTRACTS, OR ANTICIPATED SAVINGS;
* ANY LOSS, ALTERATION, CORRUPTION, DESTRUCTION, OR UNAUTHORIZED DISCLOSURE OF DATA, SETTINGS, APPLICATION CACHES, OR WORKSPACES;
* ANY ACTIONS TAKEN BY DEVICE MANUFACTURERS, CARRIERS, PLATFORM PROVIDERS, OR LAW ENFORCEMENT BODIES REGARDING SYSTEM USAGE.

THIS LIMITATION APPLIES EVEN IF THE AUTHOR OR CONTRIBUTORS HAVE BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES, AND NOTWITHSTANDING ANY FAILURE OF ESSENTIAL PURPOSE OF ANY REMEDY.

---

## 2. Non-Affiliation Notice & Trademark Ownership

### 2.1 Statement of Non-Affiliation
**ScrcpyDeX is an independent, community-driven, open-source project.**  
ScrcpyDeX is **NOT** sponsored, affiliated with, endorsed by, certified by, maintained by, or in any way officially connected to:
* **Samsung Electronics Co., Ltd.** or any of its subsidiaries, parent companies, or affiliated entities.
* **Google LLC** or Alphabet Inc.
* **Microsoft Corporation**.
* **Genymobile SAS** or Romain Vimont.

### 2.2 Trademark Ownership & Nominative Fair Use
* **"Samsung"**, **"Samsung DeX"**, **"Galaxy"**, **"One UI"**, **"Knox"**, and all related Samsung emblems, hardware model identifiers, and logos are registered trademarks or service marks of **Samsung Electronics Co., Ltd.**
* **"Android"**, **"ADB (Android Debug Bridge)"**, **"Google"**, and related marks are trademarks of **Google LLC**.
* **"Windows"**, **"Direct3D"**, **"WinUI"**, and related marks are registered trademarks of **Microsoft Corporation**.
* **"scrcpy"** is a trademark or project identifier associated with **Genymobile SAS**.

**Legal Basis:** All mentions, references, and citations of third-party trademarks, product titles, brand names, and hardware models within this repository, its source code, documentation, scripts, and graphical user interfaces are made **strictly for nominative, functional, descriptive, and identification purposes** ("Nominative Fair Use" under United States trademark law, Article 14 of EU Trademark Directive (EU) 2015/2436, and Article 132, item I of Brazilian Industrial Property Law No. 9.279/1996). 

Such use does not imply any sponsorship, endorsement, commercial association, or quality guarantee by the trademark owners.

---

## 3. Device Safety, Platform Integrity & Knox Status

ScrcpyDeX operates strictly within standard platform developer interfaces without modifying system partitions or boot structures.

### 3.1 Knox Warranty Counter on Tested Devices (`0x0`)
* **No Bootloader Modification:** ScrcpyDeX **does not require, recommend, or perform bootloader unlocking** (`OEM Unlocking`).
* **Factory Knox Status on Tested Devices:** Because the device's cryptographic secure boot chain and kernel signing remain untouched, the Samsung Knox electronic fuse (e-fuse / `Knox Warranty Void`) flag remained **0x0 (Un-tripped)** on tested hardware.
* **Hardware-Backed Features:** On the tested Galaxy S23 device, features relying on hardware integrity (such as **Samsung Pass**, **Secure Folder**, **Samsung Health**, and biometrics) continued to function normally.

### 3.2 Non-Root Architecture (`UID 2000` Shell Context)
* **Zero Root Privilege Requirements:** ScrcpyDeX **does not require, request, execute, or exploit root privileges** (`UID 0`).
* **Developer Shell Boundary:** All on-device server components execute strictly under Android's standard developer `shell` context (`UID 2000` / `gid 2000`) via `app_process` over the official Android Debug Bridge (ADB).
* **Linux Kernel `/dev/uhid`:** Mouse virtualization utilizes standard kernel user-space HID nodes (`/dev/uhid`, group `3011` / `uhid`), as provisioned by default for developer shell users in Android, creating transient hardware input devices recognized natively by Android's `EventHub`.

### 3.3 Read-Only System Partition Integrity
* **No Partition Modifications:** ScrcpyDeX **never modifies, patches, remounts as read-write (`mount -o remount,rw`), or writes to** any platform image or system partition, including `/system`, `/vendor`, `/product`, `/system_ext`, or `/odm`.
* **Zero Firmware Alterations:** The operating system binary image remains identical to factory stock firmware, fully passing Google SafetyNet / Android Play Integrity checks.
* **Ephemeral Memory Footprint:** The server JAR is temporarily staged in `/data/local/tmp` (a standard developer staging directory), executes strictly in volatile memory (RAM), and creates no persistent system daemons or resident background services once the session is terminated.

---

## 4. Statutory Legal Basis for Interoperability & Reverse Engineering

ScrcpyDeX is legitimate, lawful interoperability software developed under clear statutory exemptions and judicial precedents in major global legal jurisdictions.

### 4.1 United States Law
1. **17 U.S.C. § 1201(f) (Reverse Engineering for Interoperability):**  
   The Digital Millennium Copyright Act (DMCA) explicitly permits a person who has lawfully obtained the right to use a copy of a computer program to identify and analyze elements of that program necessary to achieve interoperability of an independently created computer program with other programs, provided such acts do not otherwise constitute copyright infringement.
2. **17 U.S.C. § 107 (Fair Use Doctrine):**  
   * *Google LLC v. Oracle America, Inc.*, 141 S. Ct. 1183 (2021) (SCOTUS): The Supreme Court confirmed that declaring code, API method signatures, and functional interface contracts reproduced solely to enable programmers to access underlying functional capabilities constitute fair use as a matter of law.
   * *Sony Computer Entertainment, Inc. v. Connectix Corp.*, 203 F.3d 596 (9th Cir. 2000): Intermediate analysis undertaken to understand functional elements and produce an independent, non-infringing emulator constitutes protected fair use.
   * *Sega Enterprises Ltd. v. Accolade, Inc.*, 977 F.2d 1510 (9th Cir. 1992): Disassembly of object code to discover functional interface specifications necessary for interoperability is fair use.
3. **No DRM Circumvention:**  
   ScrcpyDeX does not circumvent technological protection measures (TPMs), encryption keys, or copy protection schemes. It communicates with standard system display servers over open developer protocols.

### 4.2 European Union Law
1. **Directive 2009/24/EC of the European Parliament and of the Council (Computer Programs Directive):**
   * **Article 6 (Decompilation for Interoperability):** The authorization of the copyright holder is not required where reproduction of the code and translation of its form are indispensable to obtain the information necessary to achieve the interoperability of an independently created computer program with other programs, provided the acts are performed by an authorized user, the information was not previously readily available, and the acts are confined to parts strictly necessary for interoperability.
   * **Article 8(2) (Non-Waivable Public Policy):** Any contractual provisions (such as End User License Agreements or EULAs) contrary to Article 6 are null, void, and unenforceable.
   * **Article 5(3) (Right to Observe, Study, and Test):** A licensee is entitled to observe, study, or test the functioning of a program in order to determine the ideas and principles which underlie any element of the program.
2. **Court of Justice of the European Union (CJEU) Precedents:**  
   * *SAS Institute Inc. v. World Programming Ltd* (Case C-406/10): The CJEU ruled that neither the functionality of a computer program nor the programming language or format of data files constitutes a form of expression protected by copyright.

### 4.3 Brazilian Law
1. **Lei Federal nº 9.609/1998 (Software Law):**
   * **Article 6, item I:** Reproduction of legitimately acquired copies is lawful when necessary for program utilization.
   * **Article 6, item III & § 1:** Functional similarities arising from application requirements or lack of technical alternatives are fully permitted (merger doctrine and functional interoperability).
   * **Article 6, item IV:** Integration of an independent program into an existing operating system or environment is expressly protected by law.
2. **Lei Federal nº 9.610/1998 (Copyright Law):**
   * **Article 8, item I:** Ideas, normative concepts, systems, methods, or projects are excluded from copyright protection.

---

## 5. Clean-Room Implementation & Cryptographic Audit Verification

### 5.1 Strict Clean-Room Development
ScrcpyDeX enforces strict clean-room engineering standards:
* **Zero Proprietary Binary Redistribution:** ScrcpyDeX does **NOT** redistribute, package, ship, or mirror any Samsung proprietary JAR files (e.g., `framework.jar`, `services.jar`), deodexed classes, `.odex` / `.vdex` bytecode caches, or native ARM/ARM64 `.so` libraries.
* **Dynamic Java Runtime Reflection:** All calls to Samsung-specific subsystem classes (e.g., `android.hardware.display.SemWifiDisplayConfig$Builder`, `android.hardware.display.DisplayManagerGlobal`) are performed purely via dynamic reflection (`java.lang.reflect.Method.invoke()`, `Class.forName()`). The compiled `scrcpydex-server.jar` contains exclusively original Java class implementations under the namespace `com.scrcpydex.server.*`.
* **Execution Against Pre-Installed Resident Code:** Reflected methods and classes resolve exclusively against system libraries already present on the user's legally purchased device firmware.
* **Separation of Executables:** Upstream binaries (`scrcpy.exe`, `adb.exe`, `ffplay.exe`) are not bundled inside ScrcpyDeX code, but invoked independently via standard operating system subprocess calls.

### 5.2 Cryptographic Audit Verification
To ensure full transparency, non-tampering, and auditability, the server JAR distributed with ScrcpyDeX matches the following verifiable cryptographic specification:

* **File Name:** `scrcpydex-server.jar`
* **File Size:** `18,993 bytes`
* **Cryptographic Hash (SHA-256):**  
  `AEB4329BEC1AD16F051C2FA0CE415B33A1C99B078D323B27B0D7A0860A930C6F`
* **Internal Entries:** Strictly 1 file (`classes.dex`, 38,244 bytes uncompressed)
* **Bytecode Namespace:** Exclusively `com/scrcpydex/server/*`

**Verification Command (Windows PowerShell):**
```powershell
Get-FileHash -Path server\scrcpydex-server.jar -Algorithm SHA256
```

---

## 6. User Acknowledgment & Express Consent

By cloning, downloading, building, executing, packaging, or utilizing ScrcpyDeX, you explicitly acknowledge and agree that:
1. You have read, understood, and consented to all terms and conditions set forth in this Disclaimer.
2. You assume full and sole legal and technical responsibility for your use of the software.
3. You represent and warrant that you are authorized to use developer options on the connected target device.
4. You agree to indemnify and hold harmless the author, contributors, and maintainers from any damages, liabilities, claims, or costs resulting from your execution or deployment of this software.
