# Research Ethics, Safety, and Data Governance

This repository is a technical prototype, not institutional ethics approval. Before collecting data from any person, the team must obtain approval for the research protocol, consent process, inclusion and exclusion criteria, stop rules, debrief, retention schedule, and responsible personnel.

## 1. Separate training from research

Training mode provides direct instructions and is therefore unsuitable as evidence of unprompted behavior. Research mode uses neutral prompts and a validated recorded motion profile. Neutral wording alone does not guarantee experimental validity; the research advisor must approve conditions, counterbalancing, outcome definitions, and analysis.

## 2. Data minimization

- Use a code such as `P017`, never a name, student ID, email, or phone number.
- Store the code-to-identity mapping outside the Unity workstation and repository.
- Never commit or attach JSONL logs to GitHub.
- Define access rights, encryption, retention, deletion, and breach response before collection.
- Pose telemetry can be behaviorally sensitive; collect it only when required by the research question.
- Do not collect eye tracking, audio, or video unless separately justified and approved.

## 3. Freeze these variables for each study version

- Commit hash and application version
- Room layout and hazard positions
- Quake profile, axes, units, preprocessing, and floor response
- Headset model, runtime, refresh rate, and locomotion mode
- Prompt text, language, audio, and lighting
- Timing, health, damage, and cover multiplier

If any variable changes during collection, increment the scenario version and document the protocol deviation.

## 4. Minimum stop rules

Stop immediately when the participant asks or when dizziness, nausea, headache, loss of balance, panic, unusual breathing, or another safety concern appears. Stopping is not participant failure. Do not request an explanation before ending the simulation.

## 5. Claims that this prototype cannot support

- In-game magnitude is equivalent to a real earthquake magnitude.
- A floor-level effect is structurally accurate without calibration.
- Completing the tutorial makes a person safer during a real emergency.
- The room reproduces an actual Chulalongkorn University building.
- A small convenience sample represents the wider population.

## 6. Proposed research measures

Primary behavioral measures may include time to first protective action, cover entry, unsafe exit attempt, evacuation completion time, task state at onset, collision count, and final outcome. Self-reported presence, perceived realism, discomfort, and perceived risk may be collected through an approved post-test questionnaire. Measures and hypotheses must be fixed before analyzing study data.

