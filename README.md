# AI plays Donkey Kong

### Install Unity

[Unity 6 (6000.0.27f1) Download](https://unity.com/releases/editor/whats-new/6000.0.27#installs)

### Install ML-Agents Toolkit (Windows)

[For linux it should be a similar instalation process]

```powershell
# In this project miniconda was used but its the same process for python venv
conda create -n mlagents python=3.10.12 # for mlagents 22 (oct/5/2024)
conda activate mlagents

# If using CUDA platform
pip3 install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121

# If using AMD ROC platform (only supported in linux)
pip3 install torch~=2.2.1 torchvision torchaudio --index-url https://download.pytorch.org/whl/rocm6.2

# If not using GPU acceleration (CPU only)
pip3 install torch~=2.2.1 torchvision torchaudio

python -m pip install mlagents==1.1.0 # If downloading from PyPi, otherwise download it from the ML-Agents gh repo

mlagents-learn --help # verify if it was installed correctly
# if any warnings are shown download necessary cuda packages for GPU training
```
