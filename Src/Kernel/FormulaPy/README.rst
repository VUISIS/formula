Installation
------------

From Git using Conda
~~~~~~~~~~~~~~~~~~~~

To install ``formula_kernel`` from git into a Conda environment::

    git clone git@github.com:VUISIS/formula.git
    cd Src/Kernel/FormulaPy/formula_kernel
    conda create -n ker jupyter
    conda activate ker
    pip install .


Using the Formula kernel
---------------------
**Notebook**: The *New* menu in the notebook should show an option for an Formula notebook.

**Console frontends**: To use it with the console frontends, add ``--kernel formula`` to
their command line arguments.