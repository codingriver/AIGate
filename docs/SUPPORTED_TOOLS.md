# Gate 支持的工具完整列表

> 最后更新：2026-03-10  
> 当前支持：**200+** 个工具

---

## 版本控制

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| git | Git 版本控制 | ~/.gitconfig |
| svn | Subversion | ~/.subversion/servers |
| mercurial / hg | Mercurial VCS | ~/.hgrc |
| fossil | Fossil SCM | ~/.fossil |
| darcs | Darcs VCS | ~/.darcs/defaults |

## 包管理器

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| npm | Node.js 包管理器 | ~/.npmrc |
| pnpm | 快速 Node 包管理 | ~/.npmrc |
| yarn | Yarn 包管理器 | ~/.yarnrc.yml |
| pip | Python 包管理器 | pip.conf |
| pip3 | Python3 pip | pip.conf |
| conda | Anaconda 包管理 | ~/.condarc |
| gem | Ruby 包管理器 | ~/.gemrc |
| composer | PHP 包管理器 | ~/.composer/config.json |
| cargo | Rust 包管理器 | ~/.cargo/config.toml |
| pub | Dart/Flutter 包管理 | ~/.pub-cache |
| nuget | .NET 包管理器 | NuGet.Config |
| poetry | Python Poetry | config.toml |
| pipenv | Python Pipenv | 环境变量 |
| pdm | Python PDM | config.toml |
| uv | Python uv | uv.toml |
| bun | Bun JS 运行时 | bunfig.toml |
| deno | Deno 运行时 | DENO_DIR |
| hex | Elixir 包管理 | ~/.hex/hex.config |
| mix | Elixir Mix | 环境变量 |
| opam | OCaml 包管理 | ~/.opam |
| cabal | Haskell Cabal | ~/.cabal/config |
| stack | Haskell Stack | ~/.stack/config.yaml |
| leiningen | Clojure 包管理 | ~/.lein/profiles.clj |
| gradle | Gradle 构建工具 | ~/.gradle/gradle.properties |
| maven | Maven 构建工具 | ~/.m2/settings.xml |
| sbt | Scala 构建工具 | ~/.sbt/1.0/global.sbt |
| coursier | Scala 课程管理 | 环境变量 |
| conan | C++ 包管理器 | ~/.conan/conan.conf |
| vcpkg | C++ vcpkg | 环境变量 |
| homebrew | macOS 包管理器 | 环境变量 |
| apt | Debian 包管理器 | /etc/apt/apt.conf |
| yum | RHEL 包管理器 | /etc/yum.conf |
| dnf | Fedora 包管理器 | /etc/dnf/dnf.conf |
| zypper | openSUSE 包管理 | /etc/zypp/zypper.conf |
| pacman | Arch 包管理器 | /etc/pacman.conf |
| snap | Snap 包管理器 | 环境变量 |

## 容器与编排

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| docker | Docker 容器 | ~/.docker/config.json |
| docker-compose | Docker Compose | 环境变量 |
| podman | Podman 容器 | ~/.config/containers/containers.conf |
| buildah | OCI 镜像构建 | 环境变量 |
| skopeo | 镜像工具 | 环境变量 |
| kubectl | Kubernetes CLI | ~/.kube/config |
| helm | Kubernetes 包管理 | 环境变量 |
| helmfile | Helm 声明式管理 | 环境变量 |
| kind | 本地 K8s 集群 | 环境变量 |
| minikube | 本地 K8s | 环境变量 |
| k3s | 轻量 K8s | 环境变量 |
| k3d | k3s in Docker | 环境变量 |
| microk8s | MicroK8s | 环境变量 |
| rancher | Rancher CLI | 环境变量 |
| oc | OpenShift CLI | 环境变量 |
| nerdctl | containerd CLI | 环境变量 |
| lima | Linux VM | 环境变量 |
| colima | macOS 容器 | 环境变量 |

## CI/CD

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| jenkins | Jenkins CI | 环境变量 |
| github-actions | GitHub Actions | 环境变量 |
| gitlab-ci | GitLab CI | 环境变量 |
| argocd | ArgoCD | 环境变量 |
| flux | FluxCD | 环境变量 |
| tekton | Tekton Pipelines | 环境变量 |
| circleci | CircleCI CLI | ~/.circleci/cli.yml |
| travis | Travis CI | 环境变量 |
| drone | Drone CI | 环境变量 |
| woodpecker | Woodpecker CI | 环境变量 |
| buildkite | Buildkite Agent | 环境变量 |
| concourse | Concourse CI | 环境变量 |
| spinnaker | Spinnaker | 环境变量 |

## 基础设施即代码

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| terraform | HashiCorp Terraform | 环境变量 |
| terraformcli | Terraform CLI | 环境变量 |
| ansible | Ansible 自动化 | ansible.cfg |
| vault | HashiCorp Vault | 环境变量 |
| packer | HashiCorp Packer | 环境变量 |
| vagrant | HashiCorp Vagrant | 环境变量 |
| pulumi | Pulumi IaC | 环境变量 |
| cdk | AWS CDK | 环境变量 |
| bicep | Azure Bicep | 环境变量 |
| crossplane | Crossplane | 环境变量 |
| opentofu | OpenTofu | 环境变量 |

## 云 CLI

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| awscli | AWS CLI | ~/.aws/config |
| gcloud | Google Cloud SDK | ~/.config/gcloud |
| az | Azure CLI | ~/.azure |
| doctl | DigitalOcean CLI | ~/.config/doctl |
| heroku | Heroku CLI | ~/.netrc |
| flyctl | Fly.io CLI | 环境变量 |
| railway | Railway CLI | 环境变量 |
| vercel | Vercel CLI | ~/.config/configstore |
| netlify | Netlify CLI | ~/.config/netlify |
| cloudflare | Cloudflare CLI | 环境变量 |
| linode | Linode CLI | 环境变量 |
| vultr | Vultr CLI | 环境变量 |
| hcloud | Hetzner Cloud CLI | 环境变量 |
| scaleway | Scaleway CLI | 环境变量 |
| oci | Oracle Cloud CLI | 环境变量 |
| ibmcloud | IBM Cloud CLI | 环境变量 |

## 版本控制平台 CLI

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| gh | GitHub CLI | ~/.config/gh |
| glab | GitLab CLI | ~/.config/glab-cli |
| bitbucket | Bitbucket CLI | 环境变量 |
| hub | Hub (GitHub) | ~/.config/hub |

## 网络工具

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| curl | cURL | ~/.curlrc |
| wget | GNU Wget | ~/.wgetrc |
| aria2 | 下载工具 | aria2.conf |
| httpie | HTTP 客户端 | ~/.config/httpie |
| xh | 现代 HTTP 客户端 | 环境变量 |
| tailscale | Tailscale VPN | 环境变量 |
| cloudflared | Cloudflare Tunnel | 环境变量 |
| wireguard | WireGuard VPN | 环境变量 |
| openssl | OpenSSL | 环境变量 |
| ssh | SSH 客户端 | ~/.ssh/config |

## AI 云服务 API

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| openai | OpenAI API | 环境变量 |
| anthropic | Anthropic Claude | 环境变量 |
| azure-ai | Azure OpenAI | 环境变量 |
| google-ai | Google AI Studio | 环境变量 |
| mistral | Mistral AI | 环境变量 |
| groq | Groq API | 环境变量 |
| cohere | Cohere API | 环境变量 |
| perplexity | Perplexity API | 环境变量 |
| ai21 | AI21 Labs | 环境变量 |
| replicate | Replicate API | 环境变量 |
| fireworks | Fireworks AI | 环境变量 |
| together | TogetherAI | 环境变量 |
| deepinfra | DeepInfra | 环境变量 |
| anyscale | Anyscale | 环境变量 |
| openrouter | OpenRouter | 环境变量 |
| novita | NovitaAI | 环境变量 |
| hyperbolic | Hyperbolic | 环境变量 |
| lepton | Lepton AI | 环境变量 |
| cerebras | Cerebras | 环境变量 |
| sambanova | SambaNova | 环境变量 |
| xai | xAI Grok | 环境变量 |
| meta-ai | Meta AI | 环境变量 |
| inflection | Inflection AI | 环境变量 |
| poe | Poe CLI | 环境变量 |
| beam | Beam Cloud | 环境变量 |
| vertexai | Google Vertex AI | 环境变量 |
| bedrock | AWS Bedrock | ~/.aws/config |
| ai-studio | Google AI Studio | 环境变量 |
| huggingface | HuggingFace Hub | ~/.huggingface |
| hf-inference | HF Inference API | 环境变量 |

## AI 编程助手

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| cursor | Cursor IDE | ~/.cursor |
| windsurf | Windsurf IDE | ~/.windsurf |
| vscode | VS Code | settings.json |
| vscode-insiders | VS Code Insiders | settings.json |
| cline | Cline 插件 | 环境变量 |
| continue | Continue 插件 | ~/.continue |
| codeium | Codeium 插件 | 环境变量 |
| tabby | Tabby 助手 | ~/.tabby |
| aider | Aider CLI | 环境变量 |
| goose | Goose AI | 环境变量 |
| opencode | OpenCode | 环境变量 |
| bolt | Bolt.new | 环境变量 |
| sourcegraph-cody | Sourcegraph Cody | 环境变量 |
| augment | Augment Code | 环境变量 |
| copilot | GitHub Copilot | 环境变量 |
| tabnine | TabNine | 环境变量 |
| supermaven | Supermaven | 环境变量 |
| devin | Devin AI | 环境变量 |
| claude-cli | Claude CLI | 环境变量 |

## 本地 LLM

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| ollama | Ollama 本地 LLM | ~/.ollama |
| lmstudio | LM Studio | 环境变量 |
| gpt4all | GPT4All | 环境变量 |
| jan | Jan AI | 环境变量 |
| llamacpp | llama.cpp | 环境变量 |
| vllm | vLLM 服务 | 环境变量 |
| textgen | Text Gen WebUI | 环境变量 |
| localai | LocalAI | 环境变量 |
| koboldcpp | KoboldCPP | 环境变量 |
| oobabooga | Oobabooga | 环境变量 |

## AI 框架

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| langchain | LangChain | 环境变量 |
| llamaindex | LlamaIndex | 环境变量 |
| haystack | Haystack | 环境变量 |
| bentoml | BentoML | 环境变量 |
| ray | Ray AI | 环境变量 |
| autogen | AutoGen | 环境变量 |
| crewai | CrewAI | 环境变量 |
| dspy | DSPy | 环境变量 |
| semantic-kernel | Semantic Kernel | 环境变量 |
| instructor | Instructor | 环境变量 |

## AI 图像/视频

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| stability | Stability AI | 环境变量 |
| midjourney | Midjourney | 环境变量 |
| dalle | DALL-E | 环境变量 |
| runway | RunwayML | 环境变量 |
| comfyui | ComfyUI | 环境变量 |
| a1111 | AUTOMATIC1111 | 环境变量 |
| fal | fal.ai | 环境变量 |
| leonardoai | Leonardo AI | 环境变量 |

## AI 语音/数字人

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| elevenlabs | ElevenLabs | 环境变量 |
| murf | Murf AI | 环境变量 |
| wellsaid | WellSaid Labs | 环境变量 |
| heygen | HeyGen 数字人 | 环境变量 |
| descript | Descript | 环境变量 |
| whisper | OpenAI Whisper | 环境变量 |

## ML 平台/监控

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| wandb | Weights & Biases | 环境变量 |
| mlflow | MLflow | 环境变量 |
| dvc | DVC 数据版本 | .dvc/config |
| lightning | Lightning AI | 环境变量 |
| paperspace | Paperspace | 环境变量 |
| coreweave | CoreWeave | 环境变量 |
| lambda | Lambda Labs | 环境变量 |
| vast | Vast.ai | 环境变量 |
| runpod | RunPod | 环境变量 |

## AI 数据/标注

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| scaleai | Scale AI | 环境变量 |
| labelbox | Labelbox | 环境变量 |
| synthesis | Synthesis AI | 环境变量 |
| roboflow | Roboflow | 环境变量 |
| snorkel | Snorkel AI | 环境变量 |

## 编程语言工具链

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| go | Go 语言 | GOENV |
| rustup | Rust 工具链 | ~/.cargo/config.toml |
| node | Node.js | 环境变量 |
| python | Python | 环境变量 |
| r | R 语言 | ~/.Rprofile |
| julia | Julia 语言 | ~/.julia |
| dotnet | .NET SDK | 环境变量 |
| java | Java | 环境变量 |
| kotlinc | Kotlin | 环境变量 |
| swift | Swift | 环境变量 |
| dart | Dart SDK | 环境变量 |
| flutter | Flutter SDK | 环境变量 |

## 移动开发

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| cocoapods | CocoaPods | ~/.cocoapods |
| fastlane | Fastlane | 环境变量 |
| swiftpm | Swift Package Manager | 环境变量 |

## Shell 与终端

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| powershell | PowerShell | $PROFILE |
| bash | Bash | ~/.bashrc |
| zsh | Zsh | ~/.zshrc |
| fish | Fish Shell | ~/.config/fish |

## 其他工具

| 工具名 | 说明 | 配置文件 |
|--------|------|----------|
| ftp | FTP 客户端 | 环境变量 |
| rsync | 文件同步 | 环境变量 |
| rclone | 云存储同步 | ~/.config/rclone |
| act | GitHub Actions 本地运行 | 环境变量 |
| earthly | Earthly 构建 | 环境变量 |
| bazel | Bazel 构建 | .bazelrc |
| cmake | CMake 构建 | 环境变量 |

---

## 统计汇总

| 分类 | 数量 |
|------|------|
| 版本控制 | 5 |
| 包管理器 | 36 |
| 容器与编排 | 19 |
| CI/CD | 13 |
| 基础设施即代码 | 11 |
| 云 CLI | 16 |
| 版本控制平台 CLI | 4 |
| 网络工具 | 10 |
| AI 云服务 API | 37 |
| AI 编程助手 | 19 |
| 本地 LLM | 10 |
| AI 框架 | 10 |
| AI 图像/视频 | 8 |
| AI 语音/数字人 | 6 |
| ML 平台/监控 | 9 |
| AI 数据/标注 | 5 |
| 编程语言工具链 | 12 |
| 移动开发 | 3 |
| Shell 与终端 | 4 |
| 其他工具 | 7 |
| **合计** | **~214** |

---

## 社区扩展

以上为内置支持工具。社区贡献的插件请参见：
- [COMMUNITY_PLUGINS.md](./COMMUNITY_PLUGINS.md) — 社区插件商店方案
- [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md) — 插件开发指南