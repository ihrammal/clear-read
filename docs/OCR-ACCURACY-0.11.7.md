# Arabic and French OCR accuracy study — 0.11.7

17 October 2026. Six synthetic printed samples, Arial 48 px, black text on white, 1800×600 pixels. Three Arabic and three French samples; language-only and ara+eng+fra profiles; accurate, fast and automatic-orientation modes. Deskew was disabled to isolate recognition. These 36 cases are a controlled regression set, not a representative real-world accuracy estimate.

The pre-fix Arabic engine discarded complete valid rows. Disabling `textord_noise_rejrows` only for profiles containing `ara` recovered the missing rows. The change applies to ordinary image OCR, confidence passes and hybrid PDF OCR through shared recognition options. It does not replace or upgrade models. Experimental best/script models did not resolve every number issue and were not shipped.

| Sample | Profile | Layout | Character error rate | Word error rate |
| --- | --- | --- | ---: | ---: |
| arabic-basic | ara | accurate | 0.00% | 0.00% |
| arabic-basic | ara | fast | 0.00% | 0.00% |
| arabic-basic | ara | automatic | 0.00% | 0.00% |
| arabic-basic | ara+eng+fra | accurate | 0.00% | 0.00% |
| arabic-basic | ara+eng+fra | fast | 0.00% | 0.00% |
| arabic-basic | ara+eng+fra | automatic | 0.00% | 0.00% |
| arabic-hamza | ara | accurate | 0.00% | 0.00% |
| arabic-hamza | ara | fast | 0.00% | 0.00% |
| arabic-hamza | ara | automatic | 0.00% | 0.00% |
| arabic-hamza | ara+eng+fra | accurate | 0.00% | 0.00% |
| arabic-hamza | ara+eng+fra | fast | 0.00% | 0.00% |
| arabic-hamza | ara+eng+fra | automatic | 0.00% | 0.00% |
| arabic-numbers | ara | accurate | 6.90% | 10.00% |
| arabic-numbers | ara | fast | 6.90% | 10.00% |
| arabic-numbers | ara | automatic | 6.90% | 10.00% |
| arabic-numbers | ara+eng+fra | accurate | 6.90% | 10.00% |
| arabic-numbers | ara+eng+fra | fast | 6.90% | 10.00% |
| arabic-numbers | ara+eng+fra | automatic | 6.90% | 10.00% |
| french-accents | fra | accurate | 0.00% | 0.00% |
| french-accents | fra | fast | 0.00% | 0.00% |
| french-accents | fra | automatic | 0.00% | 0.00% |
| french-accents | ara+eng+fra | accurate | 2.38% | 14.29% |
| french-accents | ara+eng+fra | fast | 2.38% | 14.29% |
| french-accents | ara+eng+fra | automatic | 2.38% | 14.29% |
| french-apostrophes | fra | accurate | 0.00% | 0.00% |
| french-apostrophes | fra | fast | 0.00% | 0.00% |
| french-apostrophes | fra | automatic | 0.00% | 0.00% |
| french-apostrophes | ara+eng+fra | accurate | 1.90% | 10.53% |
| french-apostrophes | ara+eng+fra | fast | 1.90% | 10.53% |
| french-apostrophes | ara+eng+fra | automatic | 1.90% | 10.53% |
| french-numbers | fra | accurate | 0.00% | 0.00% |
| french-numbers | fra | fast | 0.00% | 0.00% |
| french-numbers | fra | automatic | 0.00% | 0.00% |
| french-numbers | ara+eng+fra | accurate | 2.22% | 11.76% |
| french-numbers | ara+eng+fra | fast | 2.22% | 11.76% |
| french-numbers | ara+eng+fra | automatic | 2.22% | 11.76% |

Two Arabic text samples matched exactly under all tested profiles/modes after NFC and whitespace normalization. The Arabic numeral sample did not: the displayed ١٢٥ was misrecognized, and the output included direction-control marks. CER includes those marks; they are not silently stripped to improve the score. Never silently substitute an expected numeral into OCR output.

All three French samples matched exactly with the French-only profile. The mixed-language profile had punctuation/word errors (roughly 1.9–2.4% CER). Select French for a French document. Review numbers and names for every language. Handwriting, camera shadows, unusual fonts, columns and real-world scans need further evaluation.

Reproduce: `.tools/dotnet/dotnet.exe run --project tests/AccessibleOCRStudio.SmokeTests -c Release -- --ocr-quality-audit artifacts/ocr-quality-0.11.7`. Reference images, expected text, raw recognized text and complete JSON metrics are in that output directory. Separate full regression suite: all 150 checks passed, including two complete-row Arabic regression checks. No claim of universally precise OCR is made.
