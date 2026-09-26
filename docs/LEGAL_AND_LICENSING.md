# Legal, Licensing, and Interoperability Compliance

**Project:** ScrcpyDeX  
**Primary Author & Copyright Holder:** Aureliano Peixoto and ScrcpyDeX Contributors  
**Governing License:** Apache License, Version 2.0  
**Effective Date:** 2026  

---

## 1. Executive Summary & Legal Framework

ScrcpyDeX is an independent, open-source interoperability suite developed to enable users to interact with the desktop mode feature of their Samsung Android devices over standard USB connections using modern personal computers.

This document sets forth the comprehensive legal framework, licensing architecture, intellectual property boundaries, Clean-Room Implementation and Interoperability Analysis compliance, and contributor requirements governing the ScrcpyDeX project.

The development, compilation, and distribution of ScrcpyDeX strictly adhere to statutory exemptions and judicial precedents governing software interoperability across the United States (DMCA § 1201(f)), the European Union (Software Directive 2009/24/EC Art. 6), and Brazil (Lei de Software nº 9.609/1998 Art. 6º).

---

## 2. Project License (Apache License 2.0)

ScrcpyDeX is licensed under the **Apache License, Version 2.0** (the "License"). You may obtain a copy of the License in the root directory [`LICENSE`](../LICENSE) or at:

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
1. **Ecosystem Alignment:** Total compatibility and philosophical consistency with upstream projects, notably Genymobile's `scrcpy` and Google's Android Open Source Project (AOSP), both of which operate under Apache 2.0.
2. **Explicit Patent Grant:** Under Section 3 of the Apache License 2.0, contributors grant users an explicit, royalty-free, irrevocable patent license for contributions incorporated into the project, protecting downstream users and distributors.
3. **Permissive Redistribution:** Facilitates personal, academic, and commercial usage while preserving attribution and maintaining clear disclaimers of liability and warranty.

---

## 3. Third-Party Licenses & Attributions

ScrcpyDeX interacts with external open-source tools and platform APIs. All respective copyrights, licenses, and attributions are acknowledged herein:

### 3.1 Genymobile `scrcpy`
* **License:** Apache License, Version 2.0
* **Copyright:** © 2018–2024 Genymobile, Romain Vimont
* **Repository:** [https://github.com/Genymobile/scrcpy](https://github.com/Genymobile/scrcpy)
* **Integration Model:** ScrcpyDeX does **not** package, redistribute, fork, or alter the `scrcpy` binary in this repository. ScrcpyDeX functions as an external activator and orchestrator. In standard operation (`run-scrcpydex.ps1`), it invokes the user's pre-installed `scrcpy` executable as an isolated, independent operating system process via standard command-line parameters (e.g., `--display-id=<ID> --mouse=uhid`). This loose coupling across process boundaries fully complies with Apache 2.0 terms.

### 3.2 Google Android Open Source Project (AOSP) & Android SDK
* **License:** Apache License, Version 2.0 (userspace framework) / GNU GPL v2 (Linux kernel)
* **Copyright:** © The Android Open Source Project
* **Usage:** ScrcpyDeX server components (`server/src/com/scrcpydex/server/`) compile against Google's public `android.jar` (API Level 30–35) and are packaged using Google's `d8` dex compiler. ScrcpyDeX uses standard Android IPC interfaces (`android.os.IBinder`, `android.view.InputEvent`, `android.hardware.display.VirtualDisplay`, `android.media.MediaCodec`). No proprietary Google or AOSP source files are redistributed.

### 3.3 FFmpeg / FFplay (Optional Standalone Client Mode)
* **License:** GNU Lesser General Public License (LGPL) v2.1+ / GNU General Public License (GPL) v2+
* **Usage:** The alternative standalone client script (`client/ScrcpyDeXClient.ps1`) allows users to display raw H.264 Annex B video streams via an external, locally installed `ffplay` process. No FFmpeg static libraries or dynamic binaries are compiled into, linked with, or hosted in this repository.

---

## 4. Interoperability Analysis, Clean-Room Implementation & Legal Precedents

ScrcpyDeX was created to achieve hardware-level software interoperability, enabling owners of Samsung Galaxy smartphones to access the desktop mode interface (DeX) on personal computers via standard USB connections without relying on proprietary cloud bridges or wireless network routing.

### 4.1 Legal Basis: Interoperability Analysis & Functional Interface Compatibility

The protocol analysis of Samsung's display subsystem and the resulting implementation of ScrcpyDeX constitute lawful **Interoperability Analysis** and **Clean-Room Implementation** focused strictly on **Functional Interface Compatibility**. This approach is firmly established and protected under international copyright legislation and landmark software interoperability precedents:

#### Groundbreaking Interoperability Precedents (WINE, Samba, Google v. Oracle)
1. **WINE Project Precedent (Clean-Room API Compatibility):**  
   The WINE project established the industry benchmark for clean-room implementation: independent developers analyze documented and undocumented Microsoft Windows system APIs and implement functional compatibility layers on POSIX systems without utilizing proprietary source code. The global legal consensus confirms that implementing compatible interfaces to achieve software interoperability is fully non-infringing.
2. **Samba Suite Precedent (Network Protocol Interoperability):**  
   The Samba project independently implemented Microsoft's proprietary Server Message Block (SMB/CIFS) network protocols by observing packet exchanges and functional interface specifications. International courts and regulatory authorities (including the European Commission) repeatedly affirmed Samba's right to analyze and implement interface specifications for cross-vendor interoperability.
3. **Google LLC v. Oracle America, Inc., 141 S. Ct. 1183 (2021) (SCOTUS):**  
   The United States Supreme Court held that functional interface declarations, method names, and API signatures that define interfaces are uncopyrightable functional requirements or are protected by the Fair Use Doctrine as a matter of law when replicated solely to achieve interoperability. Re-implementing API declarations to foster technological progress and interoperability constitutes fair use.

#### Statutory Exemptions & Jurisprudence

##### United States Law
1. **17 U.S.C. § 1201(f) (DMCA Interoperability Exemption):**  
   Specifically authorizes a person who has lawfully obtained the right to use a copy of a computer program to identify and analyze elements of the program that are necessary to achieve interoperability of an independently created computer program with other programs, to the extent such acts do not constitute copyright infringement.
2. **17 U.S.C. § 107 (Fair Use Doctrine):**  
   Established by binding federal jurisprudence:
   * *Sega Enterprises Ltd. v. Accolade, Inc.*, 977 F.2d 1510 (9th Cir. 1992): Disassembly of object code to discover functional interface specifications necessary for interoperability is protected fair use as a matter of law.
   * *Sony Computer Entertainment, Inc. v. Connectix Corp.*, 203 F.3d 596 (9th Cir. 2000): Intermediate analysis undertaken to study functional elements and produce an independent, non-infringing emulator is fair use.
   * *Google LLC v. Oracle America, Inc.*, 141 S. Ct. 1183 (2021): Functional interface declarations, method names, and API signatures that define interfaces are uncopyrightable functional requirements or are subject to fair use when replicated solely to achieve interoperability.

##### European Union Law
1. **Directive 2009/24/EC (Legal Protection of Computer Programs):**
   * **Article 6 ("Interface Analysis and Interoperability Exception"):** The authorization of the rightholder shall not be required where reproduction of the code and translation of its form are indispensable to obtain the information necessary to achieve the interoperability of an independently created computer program with other programs, provided the acts are performed by a lawful user, the information has not previously been readily available, and the acts are strictly confined to the parts necessary for functional interface compatibility.
   * **Article 5(3):** A lawful user has the unconditional right to observe, study, or test the functioning of a program in order to determine the ideas and principles which underlie any element of the program.
2. **CJEU Precedent — *SAS Institute Inc. v. World Programming Ltd* (Case C-406/10):**  
   The Court of Justice of the European Union ruled that neither the functionality of a computer program nor the programming language or format of data files used to execute or access its functions constitutes a form of expression protected by copyright.

##### Brazilian Law
1. **Lei de Software (Lei Federal nº 9.609/1998):**
   * **Artigo 6º, inciso I:** A reprodução de cópia legitimamente adquirida não constitui ofensa aos direitos do titular quando necessária à utilização do programa.
   * **Artigo 6º, inciso III e § 1º:** É plenamente lícita a ocorrência de semelhança entre programas quando esta decorrer de características funcionais de sua aplicação ou quando inexiste outra forma técnica de expressá-la (consagração legislativa da *doutrina da fusão* e da liberdade de interoperabilidade funcional).
   * **Artigo 6º, inciso IV:** É expressamente permitida a integração de um programa, mantendo-se suas características essenciais, a um sistema operacional ou aplicativo, tecnicamente indispensável à sua utilização.
2. **Lei de Direitos Autorais (Lei Federal nº 9.610/1998):**
   * **Artigo 8º, inciso I:** Estabelece expressamente que ideias, conceitos normativos, sistemas, métodos, projetos ou conceitos matemáticos não são objeto de proteção como direito autoral.

---

## 5. Technical Implementation: Dynamic Reflection & Hidden APIs

ScrcpyDeX interacts with Samsung-specific Android subsystem services without redistributing any vendor code. This is achieved entirely through standard Java runtime reflection:

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
  ScrcpyDeX constructs an interoperability payload targeting `127.0.0.1:7236` (localhost loopback) using standard data structures (`String`, `int`). The parameter names and constants (e.g., `MODE_WIRELESS_DEX = 2`) constitute uncopyrightable interface definitions necessary to instruct the device's system server to instantiate a virtual display session.
* **`android.hardware.display.DisplayManagerGlobal` & `DisplayListener`:**  
  ScrcpyDeX monitors display instantiation events using standard Java dynamic proxies (`java.lang.reflect.Proxy`), detecting when the display labeled `"ScrcpyDeX"` is allocated an internal integer display ID.
* **`android.os.ServiceManager` & `android.hardware.input.IInputManager`:**  
  ScrcpyDeX locates the native system binder service to inject standard Android input events (`MotionEvent`, `KeyEvent`) into the targeted display ID.

**Crucial Compliance Fact:**  
All reflection targets resolve against classes **already pre-installed on the user's physical device hardware** by the manufacturer. ScrcpyDeX contains zero copies of Samsung's implementation code.

---

## 6. Verification Audit: Proprietary Binary & Bytecode Status

An exhaustive legal and technical audit was conducted on the ScrcpyDeX repository to verify that **NO proprietary, copyrighted Samsung code or assets are stored, packaged, or distributed**:

| Artifact Inspected | Audit Finding | Compliance Status |
| :--- | :--- | :--- |
| **`server/scrcpydex-server.jar`** | Compiled exclusively from original Java files located in `server/src/com/scrcpydex/server/` using standard Google SDK tools (`javac` with `android-35/android.jar`, optimized via `d8`). Contains solely `classes.dex` under the namespace `com.scrcpydex.server.*`. | ✅ **100% Clean-Room / Compliant** |
| **Samsung `framework.jar`** | **ABSENT.** Not committed or distributed in ScrcpyDeX. | ✅ **Compliant** |
| **Samsung `services.jar`** | **ABSENT.** Not committed or distributed in ScrcpyDeX. | ✅ **Compliant** |
| **Deodexed `.dex` / `.odex` / `.vdex` dumps** | **ABSENT.** Temporary system dumps and inspection logs remain strictly excluded and ignored. | ✅ **Compliant** |
| **Native Samsung `.so` shared libraries** | **ABSENT.** No vendor binaries or proprietary libraries are present. | ✅ **Compliant** |
| **Proprietary graphics or assets** | **ABSENT.** No Samsung UI icons, wallpapers, fonts, or assets are distributed. | ✅ **Compliant** |

---

## 7. Process Separation & CLI/ADB Orchestration

ScrcpyDeX adheres to a strict separation of concerns regarding external executables:

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
2. **Network Bridging:** Inter-process communication across PC and device occurs over standard TCP port forwarding (`adb forward tcp:27183 tcp:27183` and `adb forward tcp:27184 tcp:27184`).
3. **No Dynamic/Static Linking:** Because communication takes place exclusively across standard operating system process boundaries and network sockets, ScrcpyDeX maintains complete architectural and legal independence.

---

## 8. Trademark Disclaimer

* **Samsung**, **Samsung DeX**, **Galaxy**, and **One UI** are trademarks or registered trademarks of Samsung Electronics Co., Ltd.
* **Android** is a trademark of Google LLC.
* **Windows** is a registered trademark of Microsoft Corporation.
* **Genymobile** and **scrcpy** are trademarks or registered marks of Genymobile.

**Notice of Non-Affiliation and Nominative Fair Use:**  
ScrcpyDeX is an independent, community-driven open-source project. ScrcpyDeX is **NOT** affiliated with, authorized by, sponsored by, maintained by, or endorsed by Samsung Electronics Co., Ltd., Google LLC, Genymobile, or any of their affiliates or subsidiaries.

Any reference to third-party trademarks, product names, or logos within this repository is made strictly for identification, descriptive, and nominative purposes under nominative fair use principles to specify device compatibility and system requirements.

---

## 9. Contributor Guidelines & Clean-Room Best Practices

All contributors submitting code, documentation, or issues to ScrcpyDeX must strictly observe the following rules:

1. **Strict Clean-Room Implementation:**  
   Under no circumstances may any contributor commit or submit pull requests containing:
   * Proprietary Samsung bytecode (`.dex`, `.odex`, `.vdex`, `.jar`, `.apk`, `.so`).
   * Verbatim disassembled or extracted method bodies copied from Samsung proprietary framework jars.
   * Proprietary artwork, icons, sound effects, or trademarked media.
2. **Functional Interface Compatibility & Interoperability Analysis:**  
   Code interacting with vendor-specific system features must use dynamic runtime reflection (`Class.forName()`, `Method.invoke()`) or public Android platform APIs. Interface signatures should only declare the minimum necessary parameters required for functional interface compatibility.
3. **License Compatibility:**  
   All contributions must be original works of the contributor or licensed under the Apache License 2.0 (or a compatible permissive license like MIT / BSD-3-Clause).
4. **Notice & Takedown:**  
   If any copyright owner believes that any file or content within this project infringes upon their rights, please open an issue or contact the project maintainer with specific details for immediate review and resolution.
