Community build of PadForge 5.1.0 with the Fanatec CSL DD transport fixes and live force-feedback decoder fixes, published from rodolfogallo's fork.

## Fixed

- **Fanatec CSL DD:** resolve the correct native HID command collection and report ID, retry failed deliveries, and invalidate stale output cache state after handle recovery.
- **Azimuth 1.0.2 / Forza Horizon 2:** apply live zero-index constant-force updates without requiring the game to restart the effect.
- **Driveclub:** update a unique constant-force effect while spring, damper and vibration effects are also present. Keep pending constant-force data separate so a periodic update cannot be bound as a constant force when effects are reconfigured around pause transitions.
- Preserve the condition effect's type independently of steering, and send constant force and condition effects to their respective Fanatec slots rather than letting one replace the other.
- Preserve force-zero, actuator-disable, pause, stop and reset handling, and the earlier Azimuth behavior.

## Validation

**91 selected automated tests passed** on Windows x64. The captured Driveclub sequence reproduced four failures in the previous build before the fix. The hardware owner confirmed working FFB in Azimuth and validated the Driveclub correction on the track with a Fanatec CSL DD. The GUI and Automobilista 2 had also worked with the earlier CSL DD transport correction.

This is not a claim of support for every wheel or effect combination. Zero-index updates remain ambiguous between multiple indistinguishable effects of the same type. Pinyon Shift has not been retested with this build. Sampled diagnostics do not establish packet-by-packet losslessness; poll stalls observed during testing are not addressed here.

## Download and install

1. Download **PadForge-5.1.0.3-CSL-DD-win-x64.zip** under Assets.
2. Exit PadForge using **Exit/Sair in its system tray menu**. Closing the window alone may leave it running.
3. Back up your existing `PadForge.exe` and keep your `PadForge.xml`.
4. Extract the archive and replace only `PadForge.exe` in your existing PadForge folder. The archive contains no personal settings, drivers or game files.
5. Launch PadForge, confirm file version **5.1.0.3**, then start the game. For Driveclub, test driving, pause and resume.

The ZIP contains the portable Windows x64 executable, instructions and build provenance. **SHA256SUMS.txt** provides checksums for both the ZIP and the EXE.

To roll back, exit PadForge and restore the EXE you backed up. Your XML settings do not need to be replaced.
