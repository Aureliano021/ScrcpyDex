# Contributing to ScrcpyDeX

Thank you for your interest in contributing to ScrcpyDeX! We welcome contributions, bug reports, and enhancements from the community.

Because this project involves hardware-software interoperability and clean-room implementation, all contributors must adhere to strict legal, licensing, and clean-room engineering guidelines.

---

## 1. Clean-Room Development Standards

To preserve the intellectual property integrity of ScrcpyDeX, every contributor must follow these rules:

1. **No Proprietary Code or Binaries:**  
   Never commit, upload, or submit pull requests containing:
   * Proprietary Samsung or vendor bytecode (`.dex`, `.odex`, `.vdex`, `.jar`, `.apk`, `.so`).
   * Verbatim disassembled or decompiled method bodies extracted from vendor firmware (`framework.jar`, `services.jar`).
   * Proprietary media, icons, UI assets, wallpapers, or sound effects.

2. **Clean-Room Interoperability:**  
   Code interacting with vendor-specific system features must use dynamic runtime reflection (`Class.forName()`, `Method.invoke()`) or public Android platform APIs. Interface signatures must declare only the minimum necessary parameters required for functional interoperability.

3. **Permissive Licensing:**  
   All submitted code must be your own original work or available under an Apache 2.0-compatible permissive open-source license (such as MIT, BSD-3-Clause, or Apache-2.0).

---

## 2. Developer Certificate of Origin (DCO)

ScrcpyDeX uses the **Developer Certificate of Origin (DCO)** version 1.1 to confirm that contributors have the right to submit their contributions under the project's [Apache License, Version 2.0](../LICENSE).

### DCO 1.1 Agreement Text
```
Developer Certificate of Origin
Version 1.1

Copyright (C) 2004, 2006 The Linux Foundation and its contributors.

Everyone is permitted to copy and distribute verbatim copies of this
license document, but changing it is not allowed.

By making a contribution to this project, I certify that:

(a) The contribution was created in whole or in part by me and I
    have the right to submit it under the open source license
    indicated in the file; or

(b) The contribution is based upon previous work that, to the best
    of my knowledge, is covered under an appropriate open source
    license and I have the right under that license to submit that
    work with modifications, whether created in whole or in part
    by me, under the same open source license (unless I am
    permitted to submit under a different license), as indicated
    in the file; or

(c) The contribution was provided directly to me by some other
    person who certified (a), (b) or (c) and I have not modified
    it.

(d) I understand and agree that this project and the contribution
    are public and that a record of the contribution (including all
    personal information I submit with it, including my sign-off) is
    maintained indefinitely and may be redistributed consistent with
    this project or the open source license(s) involved.
```

### How to Sign Your Commits
Add a `Signed-off-by` trailer to every commit message. You can do this automatically with Git:

```bash
git commit -s -m "Your descriptive commit message"
```

Example commit trailer:
```
Signed-off-by: Jane Developer <jane.developer@example.com>
```

---

## 3. Pull Request Guidelines

1. **Keep PRs Focused:** Submit small, focused pull requests addressing a single issue or feature.
2. **Test on Physical Devices:** When making changes to the server or display activation logic, test on physical Samsung Galaxy hardware across supported One UI versions if possible.
3. **Update Documentation:** If your PR changes protocol messages or CLI flags, update [`docs/PROTOCOL.md`](../docs/PROTOCOL.md) and [`README.md`](../README.md).
4. **Reproducible Builds:** When modifying Java server code in `server/src/`, recompile using `server/build-server.ps1` and document any toolchain changes.

---

## 4. Reporting Security & Legal Issues

* **Security Vulnerabilities:** If you discover a security issue or unexpected privilege exposure, please open a confidential issue or contact the project maintainer directly.
* **Copyright Notice & Takedown:** If any copyright holder believes content in this project infringes upon their rights, please open an issue with specific details for immediate review and resolution.
