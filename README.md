# Th.DiagnosticOrderCatalouge
A tool for converting a diagnostic order catalouge from CSV to the following FHIR Terminology resources:
* CodeSystems (local Lab codes)
* CodeSystems (CodeSystem Supplementfor lab defined Synonyms over SNOMED or LOINC)
* ValueSet (The Order Catalouge's ValueSet)

By deafult the tool expects a CSV file with the follwing coloum structure:

| Code | DisplayName | SystemURI | Synonyms |
|---|---|---|---|
| L100000226 | Essential Fatty Acids, Plasma | http://acme-path.com/CodeSystem/pathology-local-order-codes | Arachidonic Acid Plasma, Efa (Essential Fatty Acids), Efa Fatty Acid Profile, Eicosapentanoic Acid, Epa, Essential Fatty Acids, Essential Free Fatty Acid, Fap, Fatty Acid Profile, Fatty Acids Plasma, Fatty Acids Total/Essential, Polyunsaturated Fatty Acids, Polyunsaturated Free Fatty Acid, Total Fatty Acids, Total/Essential Fatty Acids | Department : Biochemistry<br><br>Frequency : Referred test<br><br>Comments : Please refer to FATTY ACID PROFILE, PLASMA. |
| 113058009-01 | Breast Tumour Marker (Ca 15-3), Serum | http://acme-path.com/CodeSystem/pathology-local-order-codes | 153, Breast Tumour Marker, Ca153, Ca15-3, Cancer Marker, Ca-15-3, Ca-153 | Department : Endocrinology<br><br>Specimen / Container : Blood/SS tube<br><br>Frequency : Daily<br><br>Reporting Time : 24 hours<br><br>Comments : Breast Carcinoma - see Biochemistry Appendix (12.4). |
| 40939009 | Pancreatic Tumour Marker (Ca 19-9), Serum | http://snomed.info/sct | Ca199, Ca19-9, Tumour Marker, Cancer marker, Ca19.9 | Department : Endocrinology<br><br>Specimen / Container : Blood/SS tube<br><br>Reporting Time : 24 hours<br><br>Comments : Inflammatory or neoplastic conditions of mucinous epithelium - see Biochemistry Appendix (12.4). |
| 443773007 | Ca 724, Serum | http://snomed.info/sct | Ca724, Cancer marker, Ca-724 | Department : Endocrinology<br><br>Specimen / Container : Blood/SS tube<br><br>Frequency : Referred test<br><br>Reporting Time : 4 - 5 weeks<br><br>Comments : A marker for stomach tumours. See Biochemistry Appendix (12.4). Referred test. |

> It is expected that either SNOMED (http://snomed.info/sct) or LOINC(http://loinc.org) is used as a knowen international terminology, with a Local CodeSystems URI for local codes defained by the lab

> For Pathology eRequesting in Australia SNOMED (http://snomed.info/sct) is the prefered international terminology

