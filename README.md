# Donkey Kong AI

* [Informe/Report](https://github.com/maxogod/AI-Donkey-Kong/blob/main/informe.pdf)

* *Click thumbnail to watch the video!*

[<img width="360" alt="video" src="https://github.com/user-attachments/assets/0b9c67ed-763a-4b4c-ae43-a1f917045036" />](https://www.youtube.com/watch?v=5XQuDkWKL-M)

## Sneak peeks

<img width="360" src="./imgs/1.gif" alt="easy ladder" />

<img width="360" src="./imgs/2.gif" alt="hard ladder" />

## How to use

### Install Unity

[Unity 6 (6000.0.27f1) Download](https://unity.com/releases/editor/whats-new/6000.0.27#installs)

### Install Conda

[MiniConda Download](https://www.anaconda.com/download/)

### Install [Pytorch](https://pytorch.org/) and [ML-Agents](https://github.com/Unity-Technologies/ml-agents) Toolkit

```powershell
# Create from environment config file
conda env create -f ./conda_env.yaml
conda activate donkey
```

Alternatively...

```powershell
conda create -n donkey python=3.10.12 # for mlagents 22 (oct/5/2024)
conda activate donkey

# If using CUDA platform
pip3 install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121

# If using AMD ROC platform (only supported in linux)
pip3 install torch~=2.2.1 --index-url https://download.pytorch.org/whl/rocm6.2

# If not using GPU acceleration (CPU only)
pip3 install torch~=2.2.1

python -m pip install mlagents==1.1.0 # If downloading from PyPi, otherwise download it from the ML-Agents gh repo

mlagents-learn --help # verify if it was installed correctly
```
