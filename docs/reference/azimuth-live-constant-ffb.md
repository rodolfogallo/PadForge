# Azimuth: live constant-force updates with Effect Block Index zero

On Windows 11, Azimuth 1.0.2 (Forza Horizon 2) could read a PadForge Extended
wheel but produce no force at the physical Fanatec CSL DD. The PadForge GUI
and Automobilista 2 already produced force with the separately validated CSL
DD output fix. This report concerns the PID decoder, before vendor output.

## Captured sequence

The virtual wheel used the Thrustmaster TS-PC Racer profile with a customized
product name. HIDMaestro delivered these payloads without a leading Report ID:

| Report | Payload | Meaning |
| --- | --- | --- |
| `0x1C` | `04` | Device reset |
| `0x1D` | `FF` | Full device gain |
| `0x15` | `00 00 00` | Pending constant magnitude zero, EBI zero |
| `0x11` | 21 bytes, starting `01 01 FF FF`; gain `FF` | Constant effect 1, infinite duration |
| `0x1A` | `01 01 01` | Start effect 1 |
| `0x15` | `00 93 FF` | Update magnitude to -109, still EBI zero |

The last update did not need another Set Effect or Start. The decoder stored
it in `_pending`, which was consumed only by a later explicit binding. Thus
the running effect retained its initial zero magnitude and the physical
output stayed at `force=0 effect=0` despite nonzero incoming force.

## Change and bounds

For a zero-index constant update, remember a provisional live target only
when exactly one effect is tracked and it is a running constant. During
aggregation, project the pending magnitude onto that effect, including zero
to silence it. Preserve its sign and use the existing gain, direction,
actuator, duration and pause handling.

The original effect magnitude is not overwritten. If later allocation binds
the pending write to a different effect, the original value remains intact.
Multiple tracked effects, stopped effects and other effect types do not get
this inference. Existing Set Effect/Start pending binding remains in place.
This is a bounded compatibility fallback for an unaddressed update, not a
general way to identify the intended target among multiple effects.

## Validation

- The captured zero-index update after Start failed the regression test
  before the change and passes with it.
- Tests cover live zero/resume/stop, pending preallocation, explicit updates,
  multiple effects, later allocation, other types, actuator disable and reset.
- In a local 5.1.0.2 build combining this change with the prior CSL DD transport
  fix, the hardware owner confirmed working FFB on the track. Diagnostics
  showed varying positive/negative constant forces and successful native HID
  transfers. No transport failures were recorded in the observed interval.
- The diagnostic log samples output and records input format transitions;
  it cannot prove packet-by-packet losslessness. Three poll stalls of
  109–243 ms were observed; this patch does not address their cause.

No hardware output is needed for the automated tests. This change does not
include the CSL DD transport fix, private build versions, executables, local
configuration or personal log files. Other ports have not yet been validated
with this decoder change.
