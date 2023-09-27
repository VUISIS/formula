from setuptools import setup

setup(
   name='formula',
   version='2.0',
   description='',
   author='VUISIS',
   author_email='stephen.johnson@vanderbilt.edu',
   packages=['formula'],
   install_requires=['pythonnet','jupyter','langchain==0.0.239', "openai"],
   package_data={'formula': ['CommandLine/*']}
)