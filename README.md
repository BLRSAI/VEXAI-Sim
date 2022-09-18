VEXAI Simulation and Training

Simulation
1. Download Repo
2. Launch repo in latest unity version

Training
1. Install anaconda python virutal enviroment manager - conda on package managers, lookup anaconda online for windows download
2. Navigate to where you downloaded the simulation
3. Run command inside terminal in linux or anaconda powershell prompt on windows "conda create --name name python=3.6"
4. Run command "conda activate name"
2. Install required python packages to run the Unity ml-agents package
  a. Run "python -m pip install mlagents==0.26.0" inside your venv, if on windows PyTorch will have to be installed separetly, may have to run command sudo if errors 
    https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Installation.md
3. Press the play button in the unity editor
4. Run "mlagents-learn mlagents-config.yaml" to train on our hyperparameters
  Edit the mlagents-config.yaml file for tuning your own hyperparameters

Tensorboard logging
1. Open a separate terminal from the one that is training the model
2. Navigate to the directory where the simulation is, activate the virtual enviroment, and run "tensorboard --logdir /results"
3. In a browser enter "http://localhost:6006/" for your tensorboard stats
