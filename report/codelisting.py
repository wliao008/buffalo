import fnmatch
import os
import sys

matches = []

def printcode():
	for root, dirnames, filenames in os.walk('../buffalo'):
		for filename in fnmatch.filter(filenames, '*.cs'):
			matches.append(os.path.join(root, filename))

	out = open('codelisting.tex', 'w')
	for m in matches:
		with open(m) as f:
			content = f.read()
			out.write('\\begin{lstlisting}[caption={' + m +'}, label='+m+', frame=tb, basicstyle=\\scriptsize]')
			out.write(content)
			out.write('\\end{lstlisting}')
			out.write('\n\n')


def main(args):
	printcode()

if __name__ == "__main__":
	main(sys.argv[1:])
