#!/usr/bin/env python3
"""Build the editable ICE pre-project proposal report for CEVR."""

from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_TAB_ALIGNMENT, WD_TAB_LEADER
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "report" / "CEVR_ICE_PreProject_Proposal_Report.docx"

BLUE = "2E74B5"
DARK_BLUE = "1F4D78"
INK = "0B2545"
GRAY = "5B6573"
LIGHT = "F4F6F9"
PINK = "D42A6B"
WHITE = "FFFFFF"
RED = "9B1C1C"
GOLD = "7A5A00"


def set_run_font(run, name="Calibri", size=11, bold=None, italic=None, color="000000"):
    run.font.name = name
    run._element.get_or_add_rPr().rFonts.set(qn("w:ascii"), name)
    run._element.get_or_add_rPr().rFonts.set(qn("w:hAnsi"), name)
    run.font.size = Pt(size)
    run.font.color.rgb = RGBColor.from_string(color)
    if bold is not None:
        run.bold = bold
    if italic is not None:
        run.italic = italic


def shade(cell, fill):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = tc_pr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        tc_pr.append(shd)
    shd.set(qn("w:fill"), fill)


def set_cell_width(cell, dxa):
    tc_pr = cell._tc.get_or_add_tcPr()
    tc_w = tc_pr.find(qn("w:tcW"))
    if tc_w is None:
        tc_w = OxmlElement("w:tcW")
        tc_pr.append(tc_w)
    tc_w.set(qn("w:w"), str(dxa))
    tc_w.set(qn("w:type"), "dxa")


def set_cell_margins(cell, top=80, start=120, bottom=80, end=120):
    tc = cell._tc
    tc_pr = tc.get_or_add_tcPr()
    tc_mar = tc_pr.first_child_found_in("w:tcMar")
    if tc_mar is None:
        tc_mar = OxmlElement("w:tcMar")
        tc_pr.append(tc_mar)
    for tag, value in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = tc_mar.find(qn(f"w:{tag}"))
        if node is None:
            node = OxmlElement(f"w:{tag}")
            tc_mar.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def prevent_row_split(row):
    tr_pr = row._tr.get_or_add_trPr()
    tr_pr.append(OxmlElement("w:cantSplit"))


def set_repeat_table_header(row):
    tr_pr = row._tr.get_or_add_trPr()
    repeat = OxmlElement("w:tblHeader")
    repeat.set(qn("w:val"), "true")
    tr_pr.append(repeat)


def set_table_geometry(table, widths):
    table.autofit = False
    table.alignment = WD_TABLE_ALIGNMENT.LEFT
    table_pr = table._tbl.tblPr
    tbl_w = table_pr.find(qn("w:tblW"))
    if tbl_w is None:
        tbl_w = OxmlElement("w:tblW")
        table_pr.append(tbl_w)
    tbl_w.set(qn("w:w"), str(sum(widths)))
    tbl_w.set(qn("w:type"), "dxa")
    tbl_ind = table_pr.find(qn("w:tblInd"))
    if tbl_ind is None:
        tbl_ind = OxmlElement("w:tblInd")
        table_pr.append(tbl_ind)
    tbl_ind.set(qn("w:w"), "120")
    tbl_ind.set(qn("w:type"), "dxa")
    grid = table._tbl.tblGrid
    for child in list(grid):
        grid.remove(child)
    for width in widths:
        col = OxmlElement("w:gridCol")
        col.set(qn("w:w"), str(width))
        grid.append(col)
    for row in table.rows:
        prevent_row_split(row)
        for index, cell in enumerate(row.cells):
            set_cell_width(cell, widths[index])
            set_cell_margins(cell)
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER


def add_table(doc, headers, rows, widths, font_size=9.2):
    table = doc.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    for i, header in enumerate(headers):
        cell = table.rows[0].cells[i]
        shade(cell, LIGHT)
        p = cell.paragraphs[0]
        p.paragraph_format.space_after = Pt(0)
        r = p.add_run(header)
        set_run_font(r, size=font_size, bold=True, color=INK)
    set_repeat_table_header(table.rows[0])
    for row_values in rows:
        cells = table.add_row().cells
        for i, value in enumerate(row_values):
            p = cells[i].paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            r = p.add_run(str(value))
            set_run_font(r, size=font_size)
    set_table_geometry(table, widths)
    doc.add_paragraph().paragraph_format.space_after = Pt(0)
    return table


def add_page_number(paragraph):
    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    run = paragraph.add_run("Page ")
    set_run_font(run, size=9, color=GRAY)
    fld = OxmlElement("w:fldSimple")
    fld.set(qn("w:instr"), "PAGE")
    paragraph._p.append(fld)


def configure_document(doc):
    section = doc.sections[0]
    section.page_width = Inches(8.5)
    section.page_height = Inches(11)
    section.top_margin = Inches(1)
    section.bottom_margin = Inches(1)
    section.left_margin = Inches(1)
    section.right_margin = Inches(1)
    section.header_distance = Inches(0.492)
    section.footer_distance = Inches(0.492)

    styles = doc.styles
    normal = styles["Normal"]
    normal.font.name = "Calibri"
    normal._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
    normal._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
    normal.font.size = Pt(11)
    normal.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    normal.paragraph_format.space_before = Pt(0)
    normal.paragraph_format.space_after = Pt(8)
    normal.paragraph_format.line_spacing = 1.333

    for name, size, color, before, after in (
        ("Heading 1", 16, BLUE, 18, 10),
        ("Heading 2", 13, BLUE, 12, 6),
        ("Heading 3", 12, DARK_BLUE, 8, 4),
    ):
        style = styles[name]
        style.font.name = "Calibri"
        style._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
        style._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
        style.font.size = Pt(size)
        style.font.bold = True
        style.font.color.rgb = RGBColor.from_string(color)
        style.paragraph_format.space_before = Pt(before)
        style.paragraph_format.space_after = Pt(after)
        style.paragraph_format.keep_with_next = True

    for name in ("List Bullet", "List Number"):
        style = styles[name]
        style.font.name = "Calibri"
        style.font.size = Pt(11)
        style.paragraph_format.left_indent = Inches(0.375)
        style.paragraph_format.first_line_indent = Inches(-0.194)
        style.paragraph_format.space_after = Pt(4)
        style.paragraph_format.line_spacing = 1.208

    header = section.header.paragraphs[0]
    header.alignment = WD_ALIGN_PARAGRAPH.LEFT
    r = header.add_run("ICE PRE-PROJECT 2026  |  CEVR EARTHQUAKE VR PROPOSAL")
    set_run_font(r, size=8.5, bold=True, color=GRAY)
    add_page_number(section.footer.paragraphs[0])


def add_para(doc, text, bold_lead=None, italic=False, align=None, after=8):
    p = doc.add_paragraph()
    if align is not None:
        p.alignment = align
    p.paragraph_format.space_after = Pt(after)
    if bold_lead and text.startswith(bold_lead):
        r1 = p.add_run(bold_lead)
        set_run_font(r1, bold=True)
        r2 = p.add_run(text[len(bold_lead):])
        set_run_font(r2, italic=italic)
    else:
        r = p.add_run(text)
        set_run_font(r, italic=italic)
    return p


def add_bullets(doc, items):
    for item in items:
        p = doc.add_paragraph(style="List Bullet")
        r = p.add_run(item)
        set_run_font(r)


def add_numbered(doc, items):
    for item in items:
        p = doc.add_paragraph(style="List Number")
        r = p.add_run(item)
        set_run_font(r)


def add_callout(doc, label, text, color=INK):
    table = doc.add_table(rows=1, cols=1)
    table.style = "Table Grid"
    cell = table.cell(0, 0)
    shade(cell, LIGHT)
    p = cell.paragraphs[0]
    p.paragraph_format.space_after = Pt(0)
    r = p.add_run(f"{label}: ")
    set_run_font(r, bold=True, color=color)
    r = p.add_run(text)
    set_run_font(r)
    set_table_geometry(table, [9360])
    doc.add_paragraph().paragraph_format.space_after = Pt(0)


def add_heading(doc, text, level=1):
    return doc.add_heading(text, level=level)


def add_toc(doc):
    entries = [
        ("1. Introduction", 4),
        ("2. Literature and Technical Review", 6),
        ("3. Proposed System", 7),
        ("4. Proposed Research Method", 9),
        ("5. Verification and Evaluation", 11),
        ("6. Project Management", 13),
        ("7. Expected Results and Contributions", 15),
        ("8. Limitations", 15),
        ("9. Conclusion", 16),
        ("References", 16),
        ("Appendix A. Functional Requirements", 18),
        ("Appendix B. Event Data Dictionary", 18),
        ("Appendix C. Facilitator Safety Checklist", 19),
        ("Appendix D. Draft Participant Information Elements", 19),
        ("Appendix E. GitHub Definition of Done", 20),
        ("Appendix F. Submission Checklist", 20),
        ("Appendix G. Turnitin Originality Report", 21),
    ]
    for label, page in entries:
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(4)
        p.paragraph_format.tab_stops.add_tab_stop(
            Inches(6.25), WD_TAB_ALIGNMENT.RIGHT, WD_TAB_LEADER.DOTS
        )
        r = p.add_run(f"{label}\t{page}")
        set_run_font(r, size=10.5, color=INK)


def build():
    doc = Document()
    configure_document(doc)

    # Cover: proposal_centerpiece pattern.
    for _ in range(4):
        doc.add_paragraph()
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("CHULALONGKORN UNIVERSITY")
    set_run_font(r, size=12, bold=True, color=GRAY)
    p.paragraph_format.space_after = Pt(10)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("Design and Development of a Virtual Reality Earthquake Response Simulation")
    set_run_font(r, size=24, bold=True, color=INK)
    p.paragraph_format.space_after = Pt(6)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("for Human-Behavior Study in a Thai Engineering Laboratory Context")
    set_run_font(r, size=15, color=DARK_BLUE)
    p.paragraph_format.space_after = Pt(18)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("ICE Pre-Project Proposal  |  Course 2143491  |  Academic Year 2026")
    set_run_font(r, size=10.5, bold=True, color=PINK)
    p.paragraph_format.space_after = Pt(28)
    add_table(doc, ["Project administration", "Complete before submission"], [
        ("Student team", "[Insert full names and student IDs; 2-5 ICE students required]"),
        ("Project advisor", "[Insert confirmed advisor name]"),
        ("Committee member", "[Insert advisor-proposed committee member]"),
        ("Program", "Information and Communication Engineering, ISE"),
        ("Proposal date", "24 August 2026"),
        ("Proposal deadline", "20 November 2026, 23:59"),
    ], [2800, 6560], font_size=10)
    add_callout(doc, "Document status", "Complete technical proposal draft. Replace only the bracketed administrative fields, insert final device details after procurement, and attach the Turnitin report as the final page before submission.")
    doc.add_page_break()

    add_heading(doc, "Abstract", 1)
    add_para(doc, "Earthquakes create fast, uncertain decisions while falling or sliding non-structural objects can obstruct safe action. Conventional drills cannot reproduce these hazards safely or with consistent experimental control. This project proposes CEVR, a Unity and OpenXR virtual-reality simulation that places a participant in a fictional engineering teaching laboratory, assigns ordinary tasks, introduces a delayed earthquake, records protective and evacuation behavior, and provides a controlled post-event assembly objective. The technical design separates a direct-instruction Training mode from a neutral-prompt Research mode. Physical objects receive acceleration from either a deterministic tutorial preview or a validated recorded ground-motion profile; the participant camera is never shaken. The proposed study uses a 2 x 2 between-participant design to examine two calibrated shaking-intensity bands and two floor-response conditions. Primary outcomes are protective-action latency, cover choice, unsafe exit, evacuation time, collisions, and completion. Secondary measures address presence, perceived realism, and simulator sickness. The system logs anonymous semantic events and head/hand poses while enforcing emergency-stop and data-minimization rules. The expected contribution is a reproducible, culturally contextualized prototype and research workflow for examining earthquake response in a Thai university setting without exposing participants to a real hazard. The proposal explicitly excludes structural-damage prediction and requires ethics approval, device validation, power analysis, and pilot testing before human data collection.")
    add_para(doc, "Keywords: immersive virtual reality; earthquake preparedness; human behavior; emergency evacuation; serious games; Unity; OpenXR; non-structural hazards", italic=True)

    add_heading(doc, "Document Control and Course Compliance", 1)
    add_table(doc, ["Requirement", "Current status", "Required action"], [
        ("Topic confirmation by 28 Aug 2026, 23:59", "Draft topic and implementation package prepared", "Advisor must approve or revise the Moodle topic"),
        ("Group size 2-5 ICE students", "Administrative data not supplied", "Insert all members and IDs"),
        ("Advisor supervision limit: three groups", "Not confirmed", "Confirm capacity with advisor"),
        ("Proposal report by 20 Nov 2026, 23:59", "Complete draft supplied", "Revise with advisor and submit through Moodle"),
        ("Proposal conference 7-8 Dec 2026", "Presentation not yet prepared", "Create presentation after proposal approval"),
        ("Turnitin similarity below 25%", "Not yet checked", "Run Turnitin and attach report as final page"),
    ], [2900, 2900, 3560])
    add_callout(doc, "Course rule", "The orientation states that hard deadlines are non-negotiable except special cases and that late submission deducts 10 marks each time [1].", color=RED)

    add_heading(doc, "Table of Contents", 1)
    add_toc(doc)
    doc.add_page_break()

    add_heading(doc, "1. Introduction", 1)
    add_heading(doc, "1.1 Background", 2)
    add_para(doc, "During an earthquake, occupants may have only seconds to choose between protective action, movement, and evacuation. Recommended guidance emphasizes dropping, taking cover beneath sturdy furniture, and holding on until shaking stops [2]. In actual buildings, however, furniture quality, room density, overhead equipment, and available cover differ by local context. A teaching laboratory may contain tall cabinets, suspended lights, electronic equipment, benches, and narrow circulation routes. These non-structural features affect both perceived risk and feasible action.")
    add_para(doc, "Virtual reality can expose participants to a repeatable emergency scenario while preserving experimental control and avoiding physical earthquake motion. Prior work has used immersive serious games for earthquake preparedness and reported improvements in knowledge and self-efficacy [4]. Other work has used immersive virtual reality and verbal protocol analysis to investigate decisions during shaking and post-earthquake evacuation [5]. These studies demonstrate potential, but behavioral validity, simulator sickness, experimental priming, and differences between virtual and real movement remain important limitations [7, 8, 9].")
    add_para(doc, "The proposed project focuses on a Thai university engineering context. It does not reproduce a specific campus building. Instead, it examines how a compact laboratory layout, shared cover, furniture strength, overhead hazards, and floor response can be represented in a controlled prototype. This localization differentiates the project from previous hospital and generic-building simulations without claiming that a visual difference alone constitutes scientific novelty.")

    add_heading(doc, "1.2 Problem Statement", 2)
    add_para(doc, "There is no readily available, research-ready Unity stage tailored to the project's intended Thai engineering-laboratory context that simultaneously provides deterministic ground-motion playback, neutral and instructional study modes, physical-object hazards, cover detection, safe post-event evacuation logic, anonymous event logging, reproducible GitHub collaboration, and explicit VR comfort controls. A simple script that applies random force to every rigidbody is insufficient because it is difficult to reproduce, does not represent recorded acceleration, can bias outcomes through artificial cues, and may incorrectly imply structural accuracy.")

    add_heading(doc, "1.3 Aim", 2)
    add_para(doc, "The project aims to design, implement, and evaluate an immersive VR earthquake-response simulation that can support both a guided tutorial and a future approved behavioral experiment in a fictional Thai engineering laboratory.")

    add_heading(doc, "1.4 Objectives", 2)
    add_numbered(doc, [
        "Develop a complete Unity stage with ordinary laboratory tasks, delayed earthquake onset, physical hazards, protective cover, health, evacuation, and debrief states.",
        "Implement deterministic tutorial motion and a validated importer for recorded three-axis acceleration data.",
        "Avoid camera motion and forced locomotion while providing environmental audio, lighting, and object-response cues.",
        "Record anonymous behavioral events and head/hand poses in a machine-readable format.",
        "Separate direct Training prompts from neutral Research prompts to reduce experimental priming.",
        "Evaluate functional correctness, usability, comfort, performance, and research feasibility through staged verification and pilot testing.",
        "Maintain an organized GitHub repository that supports reproducible team collaboration and review."
    ])

    add_heading(doc, "1.5 Research Questions and Hypotheses", 2)
    add_table(doc, ["ID", "Question or hypothesis"], [
        ("RQ1", "How does calibrated shaking intensity affect latency and selection of protective action?"),
        ("RQ2", "How does a lower-floor versus upper-floor response profile affect perceived intensity and behavior?"),
        ("RQ3", "How do furniture location, cover capacity, and falling-object hazards influence cover and exit decisions?"),
        ("RQ4", "Can the simulation operate with acceptable usability, frame pacing, presence, and simulator-sickness scores?"),
        ("H1", "The higher-intensity condition will produce shorter protective-action latency and higher perceived urgency."),
        ("H2", "The upper-floor response condition will increase perceived motion intensity relative to the lower-floor condition."),
        ("H3", "Participants with immediately accessible sturdy cover will be more likely to use cover than to attempt an early exit."),
    ], [1100, 8260])

    add_heading(doc, "1.6 Scope and Exclusions", 2)
    add_bullets(doc, [
        "One fictional laboratory and adjacent outdoor assembly area form the MVP.",
        "The simulation models non-structural object response and participant decisions, not structural failure.",
        "Local shaking is represented by acceleration time history and calibrated floor response, not earthquake magnitude alone.",
        "Eye tracking, biometric sensing, crowds, full glass fracture, and multi-room building evacuation are excluded from the MVP.",
        "The first evaluation is a controlled pilot; claims about real-world safety effectiveness are outside scope."
    ])

    add_heading(doc, "2. Literature and Technical Review", 1)
    add_heading(doc, "2.1 Earthquake Motion and Non-Structural Hazards", 2)
    add_para(doc, "Peak ground acceleration is a useful descriptor of local shaking but does not independently describe frequency content, duration, direction, site effects, or building response. USGS ShakeMap combines observations and models to describe ground motion and shaking intensity for response and planning [3]. For this reason, CEVR stores three-axis acceleration time histories in metres per second squared and records profile identity. A reduced single-degree-of-freedom floor-response component is available, but it must be calibrated before research use.")
    add_para(doc, "The simulation distinguishes sliding, rocking/toppling, falling, and suspended-object motion through Rigidbody mass, collider geometry, center of mass, joints, and inertial acceleration. No random torque is added to make objects fail. Hazard order is serialized so repeated trials request the same release sequence.")

    add_heading(doc, "2.2 Earthquake Protective Action", 2)
    add_para(doc, "FEMA guidance recommends Drop, Cover, and Hold On for most indoor earthquake situations and emphasizes remaining under sturdy furniture until shaking stops [2]. CEVR teaches that sequence only in Training mode. Research mode does not display the instruction at onset because doing so would convert the dependent behavior into a prompted response. The project also avoids treating an early outdoor run as automatic success; the attempt is logged separately and post-shaking evacuation is evaluated as a later phase.")

    add_heading(doc, "2.3 Immersive VR for Emergency Research", 2)
    add_para(doc, "Immersive VR permits controlled exposure to complex hazards that are difficult or unethical to reproduce physically. Feng and colleagues developed an earthquake serious game and found increased preparedness knowledge and self-efficacy after training [4]. Their decision-making study showed that think-aloud protocols can expose reasoning during virtual earthquake and post-earthquake evacuation [5]. Kinateder and colleagues demonstrated how social influence can be examined in real and virtual evacuation contexts [7].")
    add_para(doc, "Nevertheless, virtual behavior is not automatically equivalent to real behavior. Comparative evacuation research reports that head-mounted VR can reproduce some outcomes, such as pre-evacuation time and exit choice, while movement and orientation differences remain [8]. The proposed work therefore describes observed behavior inside the simulation and avoids claiming direct real-world equivalence without validation.")

    add_heading(doc, "2.4 Cybersickness, Presence, and Validity", 2)
    add_para(doc, "Simulator sickness can include nausea, oculomotor discomfort, and disorientation. The Simulator Sickness Questionnaire provides a recognized symptom framework [9]. CEVR reduces visual-vestibular conflict by keeping the camera controlled only by the tracked head, shaking the environment and objects rather than the participant viewpoint, using a world-space HUD, limiting scenario duration, and providing immediate stop controls. Presence and realism should be measured separately from sickness because a visually convincing scene can still produce discomfort or behavior that differs from the real world.")

    add_heading(doc, "2.5 Thai Context and Research Gap", 2)
    add_para(doc, "A 2026 study of Thai dental students following a major regional earthquake reported preparedness and stress outcomes and found higher-floor residence associated with greater post-traumatic stress [13]. This does not prove a causal floor effect in VR, but it supports examining floor context as a relevant participant variable. Chulalongkorn University has also provided earthquake-awareness guidance following the 2025 regional event [14]. The proposed contribution is not merely Thai decoration; it is a reproducible framework for testing contextual room constraints, calibrated floor response, and behavior metrics with clear limits on inference.")

    add_heading(doc, "3. Proposed System", 1)
    add_heading(doc, "3.1 Technology Stack", 2)
    add_table(doc, ["Component", "Selected technology", "Rationale"], [
        ("Engine", "Unity 6000.3.20f1", "Pinned project version and physics/editor ecosystem"),
        ("XR runtime", "OpenXR Plugin 1.18.0", "Headset-independent runtime with project validation"),
        ("Interaction", "XR Interaction Toolkit 3.1.3", "Component-based grab, controller, and simulator support [10, 11]"),
        ("Input", "Unity Input System 1.14.2", "Desktop debug and XR action support"),
        ("Testing", "Unity Test Framework 1.6.0", "EditMode rule and data-model tests"),
        ("Collaboration", "GitHub plus Git LFS", "Text scene review, issue ownership, CI, and binary asset handling [12]"),
        ("Data", "UTF-8 JSONL", "Appendable, parseable event records with unique session files"),
    ], [1900, 2600, 4860])

    add_heading(doc, "3.2 Gameplay Sequence", 2)
    add_numbered(doc, [
        "Orientation: the participant observes the laboratory and learns locomotion and stop controls.",
        "Normal activity: the participant places a circuit module and safety canister while an unseen 30-second minimum timer runs.",
        "Earthquake: deterministic motion starts; lights flicker, rumble plays, physical objects respond, and hazards release in a fixed sequence.",
        "Protective behavior: cover-zone entry, unsafe exit, collisions, and damage are recorded.",
        "Post-event evacuation: after shaking stops, the assembly point becomes eligible for success.",
        "Outcome and debrief: the system records success, health depletion, timeout, or participant/facilitator stop."
    ])

    add_heading(doc, "3.3 Software Modules", 2)
    add_table(doc, ["Module", "Responsibility", "Research-control feature"], [
        ("GameFlowController", "Phase state machine and outcome", "Single source of phase truth"),
        ("TutorialScenarioConfig", "Timing, mode, prompts, rules", "Versionable scenario asset"),
        ("GroundMotionPlayer", "Recorded or preview acceleration", "Profile identity and deterministic seed"),
        ("InertialRigidbody", "Apply acceleration to objects", "No camera transform and no random torque"),
        ("HazardDirector", "Release five hazards", "Serialized order and time"),
        ("Cover/Assembly zones", "Protective and success triggers", "Semantic entry events"),
        ("SessionLogger", "Unique JSONL sessions", "Participant-code sanitation"),
        ("PoseTelemetrySampler", "Head and hand position at 10 Hz", "Fixed, documented sample request"),
        ("Stage Builder", "Generate scene and wiring", "Clean-clone reproducibility"),
    ], [2100, 3500, 3760])

    add_heading(doc, "3.4 Scene Content", 2)
    add_para(doc, "The generated room includes a floor, ceiling, walls, windows, concrete columns, laboratory benches, a sturdy cover table, a marked exit, an outdoor walkway, and an assembly point. Dynamic content includes two task objects, two suspended lamps, four overhead hazards, and one unsecured cabinet. The graybox appearance is intentional: interaction geometry and experimental logic are verified before licensed campus-inspired art is commissioned.")

    add_heading(doc, "3.5 Motion Model", 2)
    add_para(doc, "Recorded profiles contain regularly sampled three-axis acceleration. The importer accepts `g` or SI units, rejects non-finite values, requires increasing time, checks sampling regularity, and stores SI acceleration. Linear interpolation supplies values between samples. In research mode, startup fails if no recorded profile is assigned. A floor-response asset may transform ground acceleration through a reduced-order damped oscillator; its calibration note and profile ID are logged.")
    add_callout(doc, "Physics limitation", "The model controls non-structural object response for an interactive study. It is not finite-element structural analysis and must not be used to certify a building or predict damage.", color=GOLD)

    add_heading(doc, "3.6 Logging and Privacy", 2)
    add_para(doc, "Each run creates a new UTF-8 JSONL file under the application persistent-data directory with `FileMode.CreateNew`. Events include UTC timestamp, relative session time, sanitized participant code, scenario ID, build version, event type, and escaped JSON payload. The configuration event records whether preview motion was used, motion-profile ID, floor-response ID, and duration. Raw logs are excluded from Git and must be transferred only under the approved data-management plan.")

    add_heading(doc, "4. Proposed Research Method", 1)
    add_heading(doc, "4.1 Study Design", 2)
    add_para(doc, "The proposed main study uses a 2 x 2 between-participant design to avoid learning and habituation from repeated earthquakes. Factor A is local shaking intensity; Factor B is floor response. The provisional target is 64 participants, 16 per condition. This number is a planning value, not a completed power calculation. The final sample must be set through an advisor-approved power analysis using pilot variance and the selected primary outcome.")
    add_table(doc, ["Condition", "Intensity request", "Floor-response context", "Participants"], [
        ("A", "Lower band, approximately 0.08 g peak component", "Lower-floor profile", "16 provisional"),
        ("B", "Lower band, approximately 0.08 g peak component", "Upper-floor calibrated profile", "16 provisional"),
        ("C", "Higher band, approximately 0.16 g peak component", "Lower-floor profile", "16 provisional"),
        ("D", "Higher band, approximately 0.16 g peak component", "Upper-floor calibrated profile", "16 provisional"),
    ], [1100, 3000, 3460, 1800])
    add_para(doc, "The headset does not physically accelerate the participant. The intensity values control virtual-object acceleration, visual motion cues in the environment, lighting, and audio. Final values must pass pilot comfort and realism checks and must be described as local acceleration conditions, not earthquake magnitude.")

    add_heading(doc, "4.2 Participants", 2)
    add_para(doc, "The planned population is adult university students aged 18 years or older. Recruitment should avoid direct academic coercion and must state that participation or withdrawal has no effect on grades. Exclusion and postponement criteria should include current severe motion sickness, acute vestibular symptoms, uncorrected visual difficulty incompatible with the headset, a medical restriction against VR, or inability to understand the consent and stop procedures. Any health screening must be approved and minimized.")

    add_heading(doc, "4.3 Variables and Measures", 2)
    add_table(doc, ["Type", "Measure", "Operational definition"], [
        ("Primary", "Protective-action latency", "Seconds from earthquake onset to first crouch or valid cover entry"),
        ("Primary", "Cover behavior", "Cover entered; cover duration; first selected protection zone"),
        ("Primary", "Unsafe exit", "Assembly-zone entry before post-quake eligibility"),
        ("Primary", "Evacuation time", "Seconds from shaking end to valid assembly entry"),
        ("Secondary", "Collision/damage", "Hazard hits, protected state, applied damage, remaining health"),
        ("Secondary", "Task state", "Number and identity of tasks complete at onset"),
        ("Secondary", "Simulator sickness", "Pre/post symptom questionnaire approved by the protocol"),
        ("Secondary", "Presence and realism", "Post-test Likert scales and short comments"),
        ("Covariate", "Prior experience", "Previous VR use, earthquake experience, and safety training"),
    ], [1400, 2800, 5160])

    add_heading(doc, "4.4 Procedure", 2)
    add_numbered(doc, [
        "Obtain written consent, assign a participant code, and complete approved screening and baseline symptoms.",
        "Explain guardian boundaries, headset fit, locomotion, and the participant/facilitator stop methods.",
        "Run a neutral interaction practice scene that is separate from the earthquake outcome scene.",
        "Randomly assign one of four conditions using a concealed schedule, stratified if necessary by prior VR experience.",
        "Start the ordinary laboratory activity. Do not announce the exact earthquake onset time.",
        "Observe without coaching during the Research-mode event unless safety requires intervention.",
        "Stop immediately for participant request or safety concern.",
        "After the event, remove the headset safely, assess symptoms, administer questionnaires, and conduct a short debrief.",
        "Transfer the anonymous log according to the data plan and record technical anomalies separately."
    ])

    add_heading(doc, "4.5 Analysis Plan", 2)
    add_para(doc, "The team will first publish descriptive distributions, missingness, technical exclusions, and adverse events. Protective-action and unsafe-exit proportions can be evaluated with logistic regression using intensity, floor condition, and their interaction. Continuous latency and evacuation time can be examined with two-way linear models if assumptions are acceptable, or robust/non-parametric alternatives otherwise. Time-to-action analysis may use survival methods when participants never perform an action. Presence and sickness outcomes will be reported with confidence intervals. The pilot is intended to estimate feasibility and variability; it will not be presented as a definitive effectiveness trial.")

    add_heading(doc, "4.6 Ethics and Safety", 2)
    add_para(doc, "The protocol must receive institutional approval before recruitment. Consent must explain the simulated earthquake, falling virtual objects, intense sound, possible discomfort, collected pose data, voluntary withdrawal, and data retention. A facilitator remains present, and a clear physical area is maintained. The participant can stop without giving a reason. Training data must not be combined with neutral Research-mode data because direct instruction changes behavior.")

    add_heading(doc, "5. Verification and Evaluation", 1)
    add_heading(doc, "5.1 Verification Strategy", 2)
    add_table(doc, ["Level", "Method", "Acceptance criterion"], [
        ("Repository", "Python static verifier and CI", "Required files, package pins, JSON, metadata, and policies pass"),
        ("Unit", "Unity EditMode tests", "All rule, health, and motion-profile tests pass"),
        ("Scene", "Editor validator", "Required controller, player, task, hazard, zone, camera, and listener counts pass"),
        ("Desktop", "Thirteen functional cases", "Every state path and log result matches expected behavior"),
        ("XR simulator", "Grab, locomotion, body triggers", "Tasks, cover, and assembly work without a headset"),
        ("Headset", "OpenXR validation and device build", "Tracking, input, frame pacing, stop, and boundary pass"),
        ("Pilot", "Internal and approved participant pilot", "No blocking comfort, data, usability, or validity issue"),
    ], [1500, 3300, 4560])

    add_heading(doc, "5.2 Current Implementation Status", 2)
    add_table(doc, ["Item", "Status", "Evidence or limitation"], [
        ("Repository structure", "Implemented", "Runtime, Editor, Tests, docs, CI, and LFS rules included"),
        ("Gameplay state machine", "Implemented", "Orientation through Debrief, including stop paths"),
        ("Motion, hazards, cover, health", "Implemented", "Deterministic code and static policy check"),
        ("Anonymous logging", "Implemented", "Unique JSONL writer and pose sampler"),
        ("One-click stage builder", "Implemented", "Editor menu and structural validator included"),
        ("Static verification", "Passed", "Available authoring-environment check"),
        ("Unity compilation", "Not yet executed", "Unity Editor is unavailable in the authoring environment"),
        ("OpenXR/headset runtime", "Not yet executed", "Requires team hardware and selected target platform"),
        ("Human-participant study", "Not started", "Requires advisor, ethics, power analysis, and pilot approval"),
    ], [2700, 2100, 4560])

    add_heading(doc, "5.3 Performance and Comfort Criteria", 2)
    add_bullets(doc, [
        "No sustained frame-time violation at the headset refresh target.",
        "No earthquake-driven camera translation, rotation, or artificial head roll.",
        "One active XR Origin and one active AudioListener.",
        "No hazard starts at the initial head position.",
        "Participant and facilitator emergency-stop controls work in the device build.",
        "Post-test symptom scores and adverse events remain within advisor- and ethics-approved continuation criteria."
    ])

    add_heading(doc, "6. Project Management", 1)
    add_heading(doc, "6.1 Work Plan", 2)
    add_table(doc, ["Period", "Milestone", "Deliverable / exit criterion"], [
        ("24-28 Aug 2026", "Topic confirmation", "Advisor approval in Moodle; group and committee fields completed"),
        ("29 Aug-13 Sep", "Unity foundation", "Clean clone compiles; packages and desktop stage pass"),
        ("14-27 Sep", "XR interaction", "One XR Origin, grabs, body triggers, stop input, simulator pass"),
        ("28 Sep-11 Oct", "Recorded motion", "Importer, selected dataset, units/axes check, floor profiles"),
        ("12-25 Oct", "Instrumentation", "Event schema, data dictionary, parser, research prompts"),
        ("26 Oct-8 Nov", "Testing and pilot", "Desktop suite, device performance, internal comfort pilot"),
        ("9-15 Nov", "Proposal revision", "Advisor comments resolved; references and method frozen"),
        ("16-19 Nov", "Submission QA", "Turnitin below 25 percent; final PDF and attachment checked"),
        ("20 Nov, 23:59", "Hard deadline", "Proposal submitted on Moodle"),
        ("21 Nov-6 Dec", "Oral preparation", "Demo build, presentation, rehearsal, backup video"),
        ("7-8 Dec", "Proposal conference", "Presentation delivered in assigned slot"),
    ], [2100, 2500, 4760])

    add_heading(doc, "6.2 Proposed Team Roles", 2)
    add_table(doc, ["Role", "Responsibilities", "Assigned member"], [
        ("Project lead / research", "Advisor coordination, protocol, measures, report, schedule", "[Insert name]"),
        ("Unity systems", "State machine, motion, physics, tests, logging", "[Insert name]"),
        ("XR and UX", "OpenXR, interaction, comfort, headset builds", "[Insert name]"),
        ("Environment and assets", "Lab design, optimization, licensed art, audio", "[Insert name]"),
        ("Data and evaluation", "Log parser, statistical plan, QA, reproducibility", "[Insert name]"),
    ], [2200, 4840, 2320])

    add_heading(doc, "6.3 Resources and Budget", 2)
    add_table(doc, ["Resource", "Requirement", "Cost status"], [
        ("VR headset and controllers", "OpenXR-compatible device with stable tracking", "Confirm existing university/team hardware before purchase"),
        ("Development workstation", "Unity-capable GPU, USB/network deployment", "Team or laboratory resource"),
        ("Unity, OpenXR, XRI", "Specified package versions", "No license cost expected for student prototype; verify terms"),
        ("3D/audio assets", "Original, CC-compatible, or purchased license", "Use graybox first; record every license"),
        ("Participant support", "If approved by ethics/protocol", "To be determined with advisor"),
        ("Data storage", "Encrypted institutional location", "Use approved university resource"),
    ], [2300, 4300, 2760])

    add_heading(doc, "6.4 Risk Register", 2)
    add_table(doc, ["Risk", "Likelihood", "Impact", "Mitigation / trigger"], [
        ("Cybersickness or panic", "Medium", "High", "No camera shake, short exposure, screening, stop control, facilitator"),
        ("Headset performance failure", "Medium", "High", "Graybox first, profiler capture, device gate before pilot"),
        ("Uncalibrated physics misrepresented", "Medium", "High", "Use time history, record units, label prototype limits, advisor review"),
        ("Research prompt biases action", "Medium", "High", "Separate Training and Research modes; freeze wording"),
        ("Participant data reaches GitHub", "Low", "Critical", "Gitignore, code-only PRs, access control, incident plan"),
        ("Unity scene merge conflict", "Medium", "Medium", "Scene owner, Force Text, issue lock, UnityYAMLMerge"),
        ("Advisor/topic approval delayed", "Medium", "High", "Confirm before 28 Aug; prepare alternate scope"),
        ("Turnitin similarity exceeds 25%", "Low", "High", "Original writing, early check, attach final report"),
        ("Sample recruitment insufficient", "Medium", "Medium", "Pilot-first claims, power analysis, extend recruitment window"),
    ], [2600, 1400, 1300, 4060])

    add_heading(doc, "7. Expected Results and Contributions", 1)
    add_para(doc, "The project is expected to produce a functioning VR tutorial stage, a reproducible research-mode configuration, validated motion-import workflow, behavioral event schema, and a documented safety/testing process. The behavioral study is expected to reveal measurable variation in protective-action latency, cover use, unsafe exit, and perceived intensity across conditions. Because those outcomes have not yet been collected, this proposal does not predict statistical significance or claim effectiveness.")
    add_bullets(doc, [
        "Technical contribution: organized Unity/OpenXR implementation with deterministic physics requests and automated checks.",
        "Method contribution: separation of training instruction from neutral behavioral observation.",
        "Context contribution: a fictional Thai engineering-laboratory scenario centered on furniture and cover constraints.",
        "Reproducibility contribution: pinned dependencies, GitHub workflow, scene generator, tests, and profile-aware logs.",
        "Educational contribution: a platform that can later be evaluated as an earthquake preparedness training tool."
    ])

    add_heading(doc, "8. Limitations", 1)
    add_para(doc, "Behavior in VR may differ from action during a real earthquake because participants know they are safe, locomotion is mediated, virtual collisions do not cause injury, and the study cannot recreate social pressure, building noise, dust, power failure, or aftershocks completely. The reduced floor model is not structural analysis. A student sample limits generalizability. Between-participant assignment reduces carryover but requires a larger sample. Presence can increase engagement while also increasing discomfort. Finally, the implemented graybox stage must be compiled and tested in Unity and on the selected headset before it can be considered operational.")

    add_heading(doc, "9. Conclusion", 1)
    add_para(doc, "CEVR proposes a careful middle ground between an entertainment game and an engineering analysis tool. It uses a controlled virtual laboratory to examine observable decisions, applies motion data to objects without shaking the participant viewpoint, records reproducible events, and maintains explicit boundaries around safety and inference. The repository already provides the core architecture and a one-click stage generator; the remaining work is machine-side Unity/XR validation, calibrated condition preparation, advisor and ethics approval, and pilot-led refinement. If these gates pass, the project can provide a credible ICE pre-project foundation and a practical platform for later senior-project research.")

    add_heading(doc, "References", 1)
    references = [
        "[1] Information and Communication Engineering Program. (2026). ICE Pre-project (2026): Orientation and Course Overview. Chulalongkorn University, 13 August 2026.",
        "[2] Federal Emergency Management Agency. (2020). Earthquake Safety at Home (FEMA P-530). https://www.fema.gov/sites/default/files/2020-08/fema_earthquakes_fema-p-530-earthquake-safety-at-home-march-2020.pdf",
        "[3] U.S. Geological Survey. (n.d.). ShakeMap. https://earthquake.usgs.gov/data/shakemap/",
        "[4] Feng, Z., Gonzalez, V. A., Amor, R., Spearpoint, M., Thomas, J., Sacks, R., Lovreglio, R., & Cabrera-Guerrero, G. (2020). An immersive virtual reality serious game to enhance earthquake behavioral responses and post-earthquake evacuation preparedness in buildings. Advanced Engineering Informatics, 45, 101118. https://doi.org/10.1016/j.aei.2020.101118",
        "[5] Feng, Z., Gonzalez, V. A., Trotter, M., Spearpoint, M., Thomas, J., Ellis, D., & Lovreglio, R. (2020). How people make decisions during earthquakes and post-earthquake evacuation: Using verbal protocol analysis in immersive virtual reality. Safety Science, 129, 104837. https://doi.org/10.1016/j.ssci.2020.104837",
        "[6] Feng, Z., Gonzalez, V. A., Amor, R., Lovreglio, R., & Cabrera-Guerrero, G. (2018). Immersive virtual reality serious games for evacuation training and research: A systematic literature review. arXiv. https://arxiv.org/abs/1805.09138",
        "[7] Kinateder, M., Ronchi, E., Gromer, D., Muller, M., Jost, M., Nehfischer, M., Muhlberger, A., & Pauli, P. (2016). Social influence on evacuation behavior in real and virtual environments. Frontiers in Robotics and AI, 3, 43. https://doi.org/10.3389/frobt.2016.00043",
        "[8] Arias, S., Fahy, R., Ronchi, E., Nilsson, D., Frantzich, H., & Wahlqvist, J. (2022). A study on evacuation behavior in physical and virtual reality experiments. Fire Technology. https://portal.research.lu.se/en/publications/a-study-on-evacuation-behavior-in-physical-and-virtual-reality-ex/",
        "[9] Kennedy, R. S., Lane, N. E., Berbaum, K. S., & Lilienthal, M. G. (1993). Simulator Sickness Questionnaire: An enhanced method for quantifying simulator sickness. The International Journal of Aviation Psychology, 3(3), 203-220. https://doi.org/10.1207/S15327108IJAP0303_3",
        "[10] Unity Technologies. (2026). XR Interaction Toolkit 3.1 documentation. https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/",
        "[11] Unity Technologies. (2026). OpenXR Plugin 1.18 documentation. https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.18/",
        "[12] GitHub. (2026). Configuring Git Large File Storage. https://docs.github.com/en/repositories/working-with-files/managing-large-files/configuring-git-large-file-storage",
        "[13] Prapinjumrune, C., et al. (2026). Post-traumatic stress, awareness, and preparedness among Thai dental students after a century-scale regional earthquake. PLOS ONE, 21, e0341032. https://doi.org/10.1371/journal.pone.0341032",
        "[14] School of Global Health, Chulalongkorn University. (2025). Earthquake Awareness and Disaster Preparedness. https://sgh.md.chula.ac.th/news/5958/",
    ]
    for ref in references:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.25)
        p.paragraph_format.first_line_indent = Inches(-0.25)
        p.paragraph_format.space_after = Pt(6)
        r = p.add_run(ref)
        set_run_font(r, size=9.5)

    doc.add_page_break()
    add_heading(doc, "Appendix A. Functional Requirements", 1)
    add_table(doc, ["ID", "Requirement", "Acceptance evidence"], [
        ("FR-01", "Begin with orientation and ordinary activity before the event", "Observed phase sequence and event log"),
        ("FR-02", "Do not start the event before 30 seconds", "EditMode timing test and desktop case F02"),
        ("FR-03", "Support two validated ordinary tasks", "Correct item IDs complete each goal"),
        ("FR-04", "Apply three-axis motion to dynamic objects without camera motion", "Code policy scan and headset observation"),
        ("FR-05", "Release four overhead hazards and one cabinet deterministically", "Scene validator and repeated run"),
        ("FR-06", "Reduce damage while in valid cover", "Health test and functional case F05"),
        ("FR-07", "Reject assembly success during shaking", "Functional case F06"),
        ("FR-08", "Accept assembly after shaking", "Functional case F07"),
        ("FR-09", "Provide participant/facilitator stop", "Functional case F10 and device control"),
        ("FR-10", "Create unique anonymous session logs", "Two-run filename and JSON parse check"),
        ("FR-11", "Reject tutorial preview in Research mode", "Functional case F12"),
        ("FR-12", "Generate and validate the stage from a clean clone", "Independent reproduction by two team members"),
    ], [1050, 4760, 3550])

    add_heading(doc, "Appendix B. Event Data Dictionary", 1)
    add_table(doc, ["Event", "Trigger", "Key payload fields"], [
        ("session_started", "Log file opens", "mode, platform"),
        ("scenario_configured", "Run validates", "motionProfile, floorResponse, duration, previewMotion"),
        ("phase_changed", "State transition", "phase"),
        ("task_completed", "Correct goal receives item", "taskId"),
        ("earthquake_onset", "Motion begins", "session time"),
        ("cover_enter / cover_exit", "Body enters or exits cover", "zoneId"),
        ("player_damaged", "Hazard hit accepted", "damage, remaining, source, protected"),
        ("unsafe_exit_attempt", "Assembly entered too early", "zoneId, phase"),
        ("assembly_enter", "Assembly trigger entered", "zoneId, accepted"),
        ("tutorial_success", "Valid outcome", "healthRemaining"),
        ("tutorial_failure", "Failure completes", "reason"),
        ("trial_aborted", "Stop requested", "reason"),
        ("pose_sample", "10 Hz telemetry request", "head, left hand, right hand XYZ"),
        ("session_ended", "Writer closes", "session time"),
    ], [2300, 3300, 3760])

    add_heading(doc, "Appendix C. Facilitator Safety Checklist", 1)
    add_bullets(doc, [
        "Confirm approved protocol version, build hash, device, and assigned condition.",
        "Clear the physical play area and activate guardian or boundary protection.",
        "Disinfect and fit the headset; align virtual and real floors.",
        "Explain voluntary participation and both emergency-stop methods.",
        "Remain within sight and reach without coaching behavior.",
        "Stop immediately for request, imbalance, nausea, distress, or technical failure.",
        "Seat the participant safely before headset removal if symptoms occur.",
        "Complete post-test symptoms and debrief before the participant leaves.",
        "Transfer anonymous data and record any deviation or adverse event."
    ])

    add_heading(doc, "Appendix D. Draft Participant Information Elements", 1)
    add_para(doc, "The final participant information sheet must be approved by the responsible institutional process. It should state the study purpose in non-deceptive language, describe the VR earthquake and virtual falling objects, explain possible dizziness or emotional discomfort, identify collected pose and event data, state duration, explain voluntary withdrawal, provide contacts, and describe storage and deletion. If the exact onset is withheld to reduce anticipation bias, the consent and debrief must explain the authorized incomplete disclosure and why it is necessary.")

    add_heading(doc, "Appendix E. GitHub Definition of Done", 1)
    add_bullets(doc, [
        "Issue has priority, acceptance criteria, owner, and reviewer.",
        "Branch contains one coherent scope and no participant data.",
        "Static verifier, Unity compilation, and relevant tests pass.",
        "Scene and metadata changes are reviewed for GUID integrity.",
        "Binary assets use Git LFS and have a documented source and license.",
        "Headset-impacting changes include device evidence.",
        "Documentation and changelog reflect externally visible behavior."
    ])

    add_heading(doc, "Appendix F. Submission Checklist", 1)
    add_bullets(doc, [
        "Replace all bracketed team, advisor, committee, and hardware fields.",
        "Confirm the final topic in Moodle by 28 August 2026 at 23:59.",
        "Obtain advisor review of scope, research questions, design, and claims.",
        "Update implementation status after Unity and headset gates.",
        "Run spelling, reference, figure/table, and page-number checks.",
        "Run Turnitin early enough to revise; similarity must be below 25 percent.",
        "Export the final PDF and verify every page visually.",
        "Attach the final Turnitin report as the last page.",
        "Submit to Moodle by 20 November 2026 at 23:59 and retain proof."
    ])

    doc.add_page_break()
    add_heading(doc, "Appendix G. Turnitin Originality Report", 1)
    add_callout(doc, "Required final attachment", "Insert the official Turnitin originality report here after the final manuscript is checked. The ICE orientation requires similarity below 25 percent and requires the Turnitin report to be attached as the last page [1].", color=RED)
    add_para(doc, "This placeholder page is intentionally last. Replace it with the exported Turnitin report before the official proposal submission.", italic=True, align=WD_ALIGN_PARAGRAPH.CENTER)

    # Metadata and update-fields preference.
    doc.core_properties.title = "CEVR ICE Pre-Project Proposal Report"
    doc.core_properties.subject = "Virtual Reality Earthquake Response Simulation"
    doc.core_properties.author = "CEVR ICE Pre-Project Team"
    settings = doc.settings._element
    update = OxmlElement("w:updateFields")
    update.set(qn("w:val"), "true")
    settings.append(update)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    doc.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    build()
