TAR=tar
GZIP=gzip
LATEX=latex
PDFLATEX=pdflatex
BIBTEX=bibtex

project:
	python codelisting.py
	$(PDFLATEX) Project
	$(BIBTEX) Project
	$(PDFLATEX) Project
	$(PDFLATEX) Project
	#chmod 660 *.dvi *.aux *.pdf *.bbl *.blg *.log *.lof *.lot *.toc

#report:
#	$(PDFLATEX) Report
#	$(BIBTEX) Report
#	$(PDFLATEX) Report
#	$(PDFLATEX) Report
#	chmod 660 *.dvi *.aux *.pdf *.bbl *.blg *.log *.lof *.lot *.toc

zip:
	zip rit-cs-capstone-LaTeX.zip *.sty *.tex *.bib makefile

clean:
	/bin/rm -f *.dvi *.aux *.bbl *.blg *.log *.lof *.lot *.toc
