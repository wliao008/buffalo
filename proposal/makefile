TAR=tar
GZIP=gzip
LATEX=latex
PDFLATEX=pdflatex
BIBTEX=bibtex

proproposal:
	$(PDFLATEX) Proposal
	$(BIBTEX) Proposal
	$(PDFLATEX) Proposal
	$(PDFLATEX) Proposal
	#chmod 660 *.dvi *.aux *.pdf *.bbl *.blg *.log *.lof *.lot *.toc

clean:
	/bin/rm -f *.dvi *.aux *.bbl *.blg *.log *.lof *.lot *.toc
