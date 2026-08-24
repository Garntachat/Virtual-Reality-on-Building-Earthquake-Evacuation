# Ground-Motion CSV and Floor Response

## 1. Supported CSV format

```text
# units=g
time,ax,ay,az
0.00,0.000,0.000,0.000
0.01,0.018,0.003,-0.012
```

- Declare units before data. Supported values are `g`, `m/s2`, and `m/s^2`.
- The header must be `time,ax,ay,az`.
- Time is measured in seconds and must increase strictly.
- Sampling intervals may vary by no more than five percent from the mean; resample externally if necessary.
- The importer stores all acceleration in metres per second squared.
- X and Z are horizontal; Y is vertical. Document the source-to-Unity axis mapping.

Select `Tools > CEVR > Import Ground Motion CSV...`, save the generated asset, and assign it to `GroundMotionPlayer` through `ConfigureRecordedProfile` or the Inspector.

`Assets/CEVR/SampleData/tutorial_preview_example.csv` tests the importer only. It is synthetic and must never be described as recorded earthquake evidence.

## 2. Floor response

`FloorResponseProfile` is a reduced single-degree-of-freedom response model for prototyping. Natural period, damping, gain, and output limit require an appropriate source and calibration. Do not convert a floor number into force with a generic formula and report it as physical truth.

## 3. Research validation checklist

- Record source, event, station, component, license, and retrieval date.
- Verify units and sample rate independently.
- Document baseline correction, filtering, clipping, and resampling.
- Cross-check peak component and vector acceleration with an independent tool.
- Confirm playback duration and peak against the source.
- Confirm axis orientation and polarity in the Unity scene.
- Record profile identifier and processing version in session metadata.
- Retain the immutable source file separately from derived assets.
