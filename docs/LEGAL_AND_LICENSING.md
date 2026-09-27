# Legal, Licensing, and Interoperability Compliance

**Project:** ScrcpyDeX  
**Primary Author & Copyright Holder:** Aureliano Peixoto and ScrcpyDeX Contributors  
**Governing License:** Apache License, Version 2.0  
**Effective Date:** 2026  

> [!IMPORTANT]
> **Legal Disclaimer:**  
> This document provides a technical compliance review and engineering architectural analysis regarding software interoperability. **It does not constitute formal legal advice, an attorney-client relationship, or a binding judicial opinion.** The legal applicability of statutory interoperability exemptions, fair use doctrines, and platform terms of service depends on individual factual circumstances, applicable local jurisdictions, and user authorization.

---

## 1. Executive Summary & Compliance Framework

ScrcpyDeX is an independent, open-source interoperability tool designed to enable owners of compatible Samsung Android devices to access the desktop mode (Samsung DeX) on personal computers over standard USB connections.

This document outlines the compliance architecture, intellectual property boundaries, clean-room development practices, and contributor requirements governing the ScrcpyDeX codebase.

The project is structured to operate within established statutory exemptions and judicial precedents governing software interoperability across the United States (DMCA 17 U.S.C. § 1201(f)), the European Union (Software Directive 2009/24/EC Art. 5(3) & 6), and Brazil (Lei de Software nº 9.609/1998 Art. 6º).

---

## 2. Project License (Apache License 2.0)

ScrcpyDeX is distributed under the **Apache License, Version 2.0** (the "License"). You may obtain a copy of the License in the root directory [`LICENSE`](../LICENSE) or at:

> [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

### 2.1 Copyright Notice
```
Copyright 2026 Aureliano Peixoto and ScrcpyDeX Contributors

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

### 2.2 Rationale for Apache 2.0
Choosing the Apache License 2.0 ensures:
1. **Ecosystem Alignment:** Consistency with upstream tools, notably Genymobile's `scrcpy` and Google's Android Open Source Project (AOSP), both governed by Apache 2.0.
2. **Explicit Patent Grant:** Under Section 3 of the Apache License 2.0, contributors grant an explicit, royalty-free, irrevocable patent license for contributions incorporated into the project.
3. **Permissive Redistribution:** Facilitates personal, academic, and community usage while preserving attribution and maintaining clear disclaimers of liability and warranty.

---

## 3. Third-Party Licenses & Attributions

ScrcpyDeX interacts with external open-source tools and platform APIs. Full third-party notices, licenses, and attributions are maintained in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

### 3.1 Genymobile `scrcpy`
* **License:** Apache License, Version 2.0
* **Copyright:** © 2018–2024 Genymobile, Romain Vimont
* **Repository:** [https://github.com/Genymobile/scrcpy](https://github.com/Genymobile/scrcpy)
* **Integration Model:** ScrcpyDeX does **not** package, bundle, fork, or alter the `scrcpy` binary in this repository. ScrcpyDeX acts solely as an external session orchestrator. In standard operation (`run-scrcpydex.ps1`), it invokes the user's pre-installed `scrcpy` executable as an independent operating system process via standard CLI arguments.

### 3.2 Google Android Open Source Project (AOSP) & Android SDK
* **License:** Apache License, Version 2.0 (userspace framework) / GNU GPL v2 (Linux kernel)
* **Copyright:** © The Android Open Source Project
* **Usage:** ScrcpyDeX server components compile against Google's public `android.jar` (API Level 30–35) and are converted to Dalvik bytecode using Google's `d8` tool. ScrcpyDeX uses standard Android IPC interfaces (`IBinder`, `InputEvent`, `VirtualDisplay`, `MediaCodec`). No proprietary Google or AOSP source files are redistributed.

### 3.3 FFmpeg / FFplay (Optional Standalone Client Mode)
* **License:** GNU Lesser General Public License (LGPL) v2.1+ / GNU General Public License (GPL) v2+
* **Usage:** An alternative standalone client script (`client/ScrcpyDeXClient.ps1`) allows users to display raw H.264 streams via an external, locally installed `ffplay` process. No FFmpeg libraries or binaries are bundled, linked with, or hosted in this repository.

---

## 4. Interoperability Analysis, Clean-Room Implementation & Legal Precedents

ScrcpyDeX was created to achieve hardware and software interoperability, allowing owners of Samsung Galaxy smartphones to interact with the device's desktop mode interface (DeX) on personal computers over standard USB connections without requiring wireless network routing or proprietary cloud services.

### 4.1 Legal Basis: Functional Interface Compatibility

The protocol analysis and implementation of ScrcpyDeX focus strictly on **Functional Interface Compatibility** and **Interoperability**. This technical approach is anchored in software interoperability exceptions and established judicial precedents:

#### Interoperability Precedents (WINE, Samba, Google v. Oracle)
1. **WINE Project Precedent (Clean-Room API Compatibility):**  
   The WINE project established the industry standard for clean-room implementation: independent developers analyze documented and undocumented OS APIs and implement functional compatibility layers without utilizing proprietary source code. Implementing compatible interfaces to achieve software interoperability is an established industry practice.
2. **Samba Suite Precedent (Network Protocol Interoperability):**  
   The Samba project independently implemented SMB/CIFS network protocols by observing packet exchanges and functional interface specifications, affirming the right to analyze and implement interface specifications for cross-vendor interoperability.
3. **Google LLC v. Oracle America, Inc., 141 S. Ct. 1183 (2021) (SCOTUS):**  
   The United States Supreme Court held that functional interface declarations and method signatures that define interfaces constitute fair use as a matter of law when replicated solely to achieve interoperability and enable programmers to call underlying functionality.

#### Statutory Exemptions & Jurisprudence

##### United States Law
1. **17 U.S.C. § 1201(f) (Reverse Engineering Exception for Interoperability):**  
   Authorizes a person who has lawfully obtained the right to use a copy of a computer program to identify and analyze elements of the program that are necessary to achieve interoperability of an independently created computer program with other programs, to the extent such acts do not constitute copyright infringement.
2. **17 U.S.C. § 107 (Fair Use Doctrine):**  
   * *Sega Enterprises Ltd. v. Accolade, Inc.*, 977 F.2d 1510 (9th Cir. 1992): Analysis of object code to discover functional interface specifications necessary for interoperability is protected fair use.
   * *Sony Computer Entertainment, Inc. v. Connectix Corp.*, 203 F.3d 596 (9th Cir. 2000): Intermediate analysis undertaken to study functional elements and produce an independent, non-infringing emulator constitutes fair use.
   * *Google LLC v. Oracle America, Inc.*, 141 S. Ct. 1183 (2021): Functional interface declarations and method signatures replicated to achieve interoperability constitute fair use.

##### European Union Law
1. **Directive 2009/24/EC (Legal Protection of Computer Programs):**
   * **Article 6 ("Decompilation for Interoperability"):** The authorization of the rightholder is not required where reproduction of code or translation of its form is indispensable to obtain information necessary to achieve interoperability of an independently created program with other programs, provided:
     - The acts are performed by a licensee or authorized user;
     - The information was not previously readily available;
     - The acts are strictly confined to parts necessary to achieve interoperability.
     - *Note:* Under Article 8(2), contractual provisions contrary to Article 6 are null and void.
   * **Article 5(3):** An authorized user has the right to observe, study, or test the functioning of a program to determine the ideas and principles which underlie any element of the program.
2. **CJEU Precedent — *SAS Institute Inc. v. World Programming Ltd* (Case C-406/10):**  
   The Court of Justice of the European Union ruled that neither the functionality of a computer program nor the programming language or format of data files used to execute its functions constitutes a form of expression protected by copyright.

##### Brazilian Law
1. **Lei de Software (Lei Federal nº 9.609/1998):**
   * **Artigo 6º, inciso I:** A reprodução de cópia legitimamente adquirida não constitui ofensa aos direitos do titular quando necessária à utilização do programa.
   * **Artigo 6º, inciso III e § 1º:** É lícita a ocorrência de semelhança entre programas quando esta decorrer de características funcionais de sua aplicação ou quando inexiste outra forma técnica de expressá-la (doutrina da fusão e liberdade de interoperabilidade funcional).
   * **Artigo 6º, inciso IV:** É expressamente permitida a integração de um programa, mantendo-se suas características essenciais, a um sistema operacional ou aplicativo, tecnicamente indispensável à sua utilização.
2. **Lei de Direitos Autorais (Lei Federal nº 9.610/1998):**
   * **Artigo 8º, inciso I:** Estabelece expressamente que ideias, conceitos normativos, sistemas, métodos, projetos ou conceitos matemáticos não são objeto de proteção como direito autoral.

---

## 5. Technical Implementation: Dynamic Reflection & Hidden APIs

ScrcpyDeX interacts with Samsung-specific Android subsystem services without redistributing vendor code. This is achieved entirely through standard Java runtime reflection:

```
┌────────────────────────────────────────────────────────┐
│               ScrcpyDeX Server Runtime                │
│             (com.scrcpydex.server.*)                   │
└──────────────────────────┬─────────────────────────────┘
                           │ Dynamic Reflection
                           │ Class.forName() / Method.invoke()
                           ▼
┌────────────────────────────────────────────────────────┐
│            Device Resident Android Framework           │
│        (Samsung One UI Firmware in Device Memory)      │
│                                                        │
│  • android.hardware.display.SemWifiDisplayConfig       │
│  • android.hardware.display.IDisplayManager$Stub       │
│  • android.hardware.display.DisplayManagerGlobal       │
│  • android.os.ServiceManager                           │
│  • android.hardware.input.IInputManager                │
└────────────────────────────────────────────────────────┘
```

### 5.1 Justification of Reflected Interfaces
* **`android.hardware.display.SemWifiDisplayConfig$Builder`:**  
  ScrcpyDeX constructs an interoperability payload targeting `127.0.0.1:7236` (localhost loopback) using standard data types (`String`, `int`). The parameter names and constants (e.g., `MODE_WIRELESS_DEX = 2`) constitute functional interface parameters necessary to instruct the device's system server to instantiate a virtual display session.
* **`android.hardware.display.DisplayManagerGlobal` & `DisplayListener`:**  
  ScrcpyDeX monitors display instantiation events using standard Java dynamic proxies (`java.lang.reflect.Proxy`), detecting when the display labeled `"ScrcpyDeX"` is allocated an internal integer display ID.
* **`android.os.ServiceManager` & `android.hardware.input.IInputManager`:**  
  ScrcpyDeX locates the native system binder service to inject standard Android input events (`MotionEvent`, `KeyEvent`) into the targeted display ID.

**Crucial Technical Fact:**  
All reflection targets resolve against classes **already pre-installed on the user's physical device** by the manufacturer. ScrcpyDeX contains zero copies of Samsung's implementation code or binary libraries.

---

## 6. Verifiable Audit: Server JAR & Binary Integrity

An inspection of the repository confirms that **no proprietary Samsung code or binaries are stored, packaged, or distributed**:

| Artifact Inspected | Audit Finding | Status |
| :--- | :--- | :--- |
| **`server/scrcpydex-server.jar`** | Compiled exclusively from original Java sources in `server/src/com/scrcpydex/server/` using standard Google SDK tools (`javac` with `android-35/android.jar`, converted via `d8`). Contains solely `classes.dex` under namespace `com.scrcpydex.server.*`. | ✅ **Clean-Room Verified** |
| **Samsung `framework.jar`** | **ABSENT.** Not committed or tracked in Git. | ✅ **Compliant** |
| **Samsung `services.jar`** | **ABSENT.** Not committed or tracked in Git. | ✅ **Compliant** |
| **Deodexed `.dex` / `.odex` / `.vdex` dumps** | **ABSENT.** Excluded by `.gitignore` rules. | ✅ **Compliant** |
| **Native Samsung `.so` shared libraries** | **ABSENT.** No vendor native libraries are included. | ✅ **Compliant** |
| **Proprietary graphics or assets** | **ABSENT.** No Samsung UI icons, wallpapers, fonts, or assets are distributed. | ✅ **Compliant** |

### 6.1 Cryptographic Hash & Verification Procedure

Users and auditors can independently verify the distributed `server/scrcpydex-server.jar`:

* **File Name:** `scrcpydex-server.jar`
* **File Size:** `18,993 bytes`
* **SHA-256 Checksum:** `aeb4329bec1ad16f051c2fa0ce415b33a1c99b078d323b27b0d7a0860a930c6f`
* **Archive Contents:** Strictly 1 entry (`classes.dex`, 38,244 bytes uncompressed)

#### Verification Command (Windows PowerShell):
```powershell
Get-FileHash -Path server\scrcpydex-server.jar -Algorithm SHA256
```

#### Verification Command (Linux / macOS):
```bash
sha256sum server/scrcpydex-server.jar
```

#### Inspecting Archive Entries:
```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead("server\scrcpydex-server.jar")
$zip.Entries | Select-Object FullName, Length, CompressedLength
```

---

## 7. Process Separation & Subprocess Architecture

ScrcpyDeX maintains a strict separation of concerns regarding external executables:

```
┌───────────────────────────────┐
│        ScrcpyDeX CLI          │
│   (PowerShell Orchestrator)   │
└──────┬─────────────────┬──────┘
       │ Process Spawn   │ Process Spawn
       ▼                 ▼
┌──────────────┐  ┌──────────────┐
│   adb.exe    │  │  scrcpy.exe  │
│ (Google SDK) │  │ (Genymobile) │
└──────────────┘  └──────────────┘
```

1. **Subprocess Invocation:** `adb.exe` and `scrcpy.exe` are called via standard OS process pipelines (`Start-Process` / CLI execution).
2. **Network Bridging:** Communication across PC and device occurs over standard TCP port forwarding (`adb forward tcp:27183 tcp:27183` and `adb forward tcp:27184 tcp:27184`).
3. **No Dynamic/Static Linking:** Because communication takes place exclusively across standard operating system process boundaries and local loopback sockets, ScrcpyDeX maintains complete architectural and legal independence.

---

## 8. Trademark Disclaimer & Nominative Fair Use

* **Samsung**, **Samsung DeX**, **Galaxy**, and **One UI** are trademarks or registered trademarks of Samsung Electronics Co., Ltd.
* **Android** is a trademark of Google LLC.
* **Windows** is a registered trademark of Microsoft Corporation.
* **Genymobile** and **scrcpy** are trademarks or registered marks of Genymobile.

**Notice of Non-Affiliation:**  
ScrcpyDeX is an independent, community-driven open-source project. ScrcpyDeX is **NOT** affiliated with, authorized by, sponsored by, maintained by, or endorsed by Samsung Electronics Co., Ltd., Google LLC, Genymobile, or any of their affiliates or subsidiaries.

Any reference to third-party trademarks, product names, or logos within this repository is made strictly for identification, descriptive, and nominative purposes under nominative fair use principles to specify device compatibility and system requirements.

---

## 9. Security Model, Trust Boundaries & Operational Guidelines

### 9.1 ADB Privilege Context (UID 2000)
The ScrcpyDeX server process executes on the target Android device via:
```bash
CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server
```
* **Privilege Level:** Operates under Android `shell` user credentials (`UID 2000`).
* **Root Requirements:** Does **not** require, request, or attempt to gain `root` (UID 0) privileges.
* **Scope:** Uses standard development capabilities granted by the Android OS to authorized ADB connections (display creation, input injection).

### 9.2 Local Loopback & Transport Security
* **Local Loopback Only:** Video streaming and control sockets bind to `127.0.0.1` on both host and device.
* **USB Recommended:** The recommended and supported connection method is a direct physical USB cable with `adb forward`.
* **Wi-Fi ADB Warning:** The ScrcpyDeX control channel does not implement transport layer encryption or session token authentication. **Using Wi-Fi ADB over unencrypted or shared public Wi-Fi networks is strongly discouraged**, as unauthorized devices on the local network could theoretically connect to an exposed ADB port.

### 9.3 Device Authorization & Intended Use
* ScrcpyDeX is designed exclusively for use on devices owned or legitimately authorized for testing and personal productivity by the user.
* It must **not** be used to circumvent lockscreens, Knox enterprise security policies, digital rights management (DRM), or unauthorized third-party devices.

---

## 10. Contributor Guidelines & Clean-Room Standards

All contributors submitting code, documentation, or pull requests to ScrcpyDeX must comply with the guidelines defined in [`CONTRIBUTING.md`](../.github/CONTRIBUTING.md):

1. **Strict Clean-Room Implementation:** No proprietary vendor bytecode, disassembled proprietary method bodies, or trademarked media may be committed.
2. **Interface Compatibility:** Interactions with system features must rely on dynamic reflection or public Android APIs.
3. **Developer Certificate of Origin (DCO):** Contributions must be submitted under the DCO 1.1 agreement using standard `Signed-off-by` commit trailers.
4. **Notice & Takedown:** If any copyright holder believes content in this project infringes upon their rights, please open an issue or contact the project maintainer for immediate review and resolution.
