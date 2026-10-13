# HelloApi: a hands-on lab

**Who this is for:** someone with zero to basic knowledge who wants to understand API programming, GitHub, Azure, Key Vault, AKS and APIM well enough to talk about them confidently in an interview.

**How every stage is laid out**
- **Purpose:** what you are building and why.
- **Concepts:** the ideas in plain words.
- **Steps:** each command with what it does and why.
- **Done check:** how you know it worked.
- **Say it in the interview:** one or two sentences you can reuse.

Commands use PowerShell. Anything in `<angle brackets>` is for you to fill in. Lines starting with `#` inside a code block are comments, not part of the command.

**Cost warning:** AKS, ACR and APIM charge while they exist. Do Stage 7 (Cleanup) when you finish. A new Azure free account often covers a short lab, but check your own subscription.

---

## The big picture (read this first, 3 minutes)

You build one tiny API and move it step by step from your laptop to the cloud.

    Your laptop  ->  GitHub  ->  Azure Container Registry  ->  AKS (runs it)  ->  APIM (front door)
                                                                  |
                                                            Key Vault (secrets)

| Piece | What it is | One-line reason it exists |
|---|---|---|
| API | A program other programs call over HTTP | Lets systems share data and actions |
| GitHub | Stores code and its history | Teams share, review and track changes |
| CI (GitHub Actions) | A robot that builds on every push | Catches broken code early |
| Docker image | The app packed with what it needs | Runs the same everywhere |
| ACR | Private store for images | AKS pulls your image from here |
| AKS | Runs containers and keeps them healthy | Scaling and self-healing |
| Key Vault | A safe for secrets | No passwords in code |
| Workload identity | A login Azure gives your pod | The pod reads the vault with no password |
| APIM | A gateway in front of APIs | Keys, limits and rules in one place |

**Manager-level summary you can say:** "Code goes to GitHub, CI builds it, the image goes to a registry, AKS runs it, Key Vault holds secrets, and APIM controls access."

## 2-hour plan

| Time | What | Stage |
|---|---|---|
| 0:00-0:15 | Install tools, create accounts | 0 |
| 0:15-0:30 | Run and test the API locally | 1 |
| 0:30-0:50 | GitHub: repo, push, CI, pull request | 2 |
| 0:50-1:00 | Azure basics, build image, start AKS in the background | 3 |
| 1:00-1:15 | Key Vault (while AKS builds) | 4 |
| 1:15-1:40 | Deploy to AKS, then workload identity | 5 |
| 1:40-1:55 | APIM | 6 |
| 1:55-2:00 | Cleanup | 7 |

Short on time? Do Stages 0 to 4 and skip 5b and 6. The free parts come first.

## Words you will meet

| Word | Plain meaning |
|---|---|
| Endpoint | One URL the API answers, such as /api/hello |
| HTTP verb | The action: GET read, POST create, PUT update, DELETE remove |
| Status code | The result: 200 OK, 201 Created, 400 your mistake, 401 not signed in, 403 not allowed, 404 not found, 429 too many calls, 500 server error |
| Repository (repo) | A project folder on GitHub with full history |
| Commit | A saved snapshot of changes |
| Branch | A separate line of work so main stays safe |
| Push | Upload commits to GitHub |
| Pull request (PR) | A request to merge a branch into main, reviewed by the team |
| CI | Continuous integration: automatic build and checks on every change |
| Subscription | Your Azure billing account |
| Resource group | A folder for Azure resources; delete it and everything inside goes |
| Container | A packaged, isolated app |
| Pod | The smallest unit Kubernetes runs; holds your container |
| Deployment | Kubernetes: "keep N copies running" |
| Service | Kubernetes: one steady address in front of changing pods |
| Probe | A health check Kubernetes runs on your app |
| Identity | A login for a person or an app |
| RBAC | Role-based access: permissions given by role (for example "Secrets User") |
| Least privilege | Give only the permission that is needed |

---

## Stage 0: Setup (15 min)

**Purpose:** get accounts and tools ready so nothing blocks you later.

**Why it matters:** 80% of lab time is lost to setup problems. Doing it first and checking each tool avoids that.

**Steps**

1. **Create an Azure account** at https://azure.microsoft.com/free (or sign in).
   *Why:* all cloud resources live in an Azure subscription, which is also where the bill goes.
2. **Create a GitHub account** at https://github.com (or sign in).
   *Why:* GitHub stores your code and runs the automatic build.
3. **Install the tools** in PowerShell, then close and reopen the terminal:

       winget install Microsoft.DotNet.SDK.8   # builds and runs the API
       winget install Git.Git                  # version control
       winget install Microsoft.AzureCLI       # the "az" command for Azure

   *Why reopen the terminal:* Windows adds new programs to the PATH only for windows opened after the install. If `az` is "not recognized", this is almost always the reason.
   *Docker is not needed on your laptop:* Azure builds the image for you in Stage 3.
4. **Check each tool** (each should print a version):

       dotnet --version
       git --version
       az --version

5. **Tell Git who you are (one time):**

       git config --global user.name "Kaushik Dutta"          # keep the quotes if there is a space
       git config --global user.email "<your GitHub email>"

   *Why:* Git stamps every commit with this name and email. To find the email: GitHub, profile picture, Settings, Emails. If "Keep my email private" is ticked, use the `...@users.noreply.github.com` address shown there.
6. **Unzip** `HelloApi.zip` to a simple folder, for example `C:\work\HelloApi`.

**Done check:** all three version commands print a number.

**Say it in the interview:** "I set up the toolchain first and verified it, because environment problems are the most common delay."

---

## Stage 1: Run the API on your laptop (15 min)

**Purpose:** see a working API and understand its parts.

**Concepts**
- An **API** is a program that other programs call over the network. A browser or app sends a request (a verb plus a URL) and gets a response (a status code plus data).
- **REST** is the common style: URLs name things (/api/hello), verbs say the action, status codes say the result.
- **Swagger/OpenAPI** is an automatic test page and description of your API. APIM will use that description in Stage 6.

**Steps**

1. Open a terminal in the folder:

       cd C:\work\HelloApi
       dotnet restore   # downloads the libraries listed in HelloApi.csproj
       dotnet run       # builds and starts the API

   *Why `restore`:* the project lists packages it needs; this fetches them. (`run` does it automatically too, but it helps to know the step exists.)
2. Note the address printed, such as `http://localhost:5xxx`. Open it in a browser and add `/swagger`.
3. In Swagger click an endpoint, then **Try it out**, then **Execute**. Test each:

| Call | Expect | What it teaches |
|---|---|---|
| GET /api/hello | 200 and a message | Basic read, configuration |
| GET /api/hello/Kaushik | 200, "Hello, Kaushik!" | Route parameters |
| GET /api/hello/ plus 60 letters | 400 Bad Request | Input validation |
| POST /api/hello with `{"name":"Kaushik"}` | 201 Created | Create returns 201 |
| POST /api/hello with `{"name":""}` | 400 Bad Request | Validation on POST |
| GET /health | 200 healthy | Health check used by Kubernetes later |

4. Stop with **Ctrl+C**.

**Read the code (10 minutes).** The files are commented line by line.
- `Program.cs`: builds the app, then sets the order things happen to each request (the pipeline).
- `Controllers/HelloController.cs`: the controller style. Look for `[HttpGet]`, `[Route]`, validation, and dependency injection in the constructor.
- The `/health` line in `Program.cs`: the same idea written as a minimal API in one line.

**Controller vs minimal API:** controllers give more structure and suit big APIs; minimal APIs are lighter and suit small services. A common interview question.

**Done check:** you saw a 200, a 201 and a 400.

**Say it in the interview:** "I return correct status codes, validate input at the boundary, keep configuration outside code, and know both controller and minimal API styles."

**Homework if you have time:** make an endpoint `async` with `await Task.Delay(500)`; add a PUT and a DELETE; add an xUnit test project.

---

## Stage 2: GitHub (20 min)

**Purpose:** keep your code safe, track every change, and have a robot check it automatically.

**Why it matters:** engineering teams work through branches and pull requests, never by editing main directly. Describing this flow shows you understand how teams ship safely.

**2a. Create an empty repository**

1. On github.com click **+** (top right), then **New repository**.
2. Name it `HelloApi`, choose **Private**, and leave README, .gitignore and license **unticked**.
   *Why unticked:* your folder already has its own files; adding more on GitHub causes a conflict on your first push.
3. Click **Create repository**.

**2b. Push your code**

    git init                       # turn this folder into a Git repository
    git add .                      # stage all files for the next snapshot (.gitignore skips bin/, obj/, secrets)
    git commit -m "Initial hello API"   # save the snapshot with a message
    git branch -M main             # name the main branch "main"
    git remote add origin https://github.com/<your-username>/HelloApi.git   # link to the GitHub repo
    git push -u origin main        # upload

A browser window may ask you to sign in to GitHub the first time. Allow it. Refresh the repo page: your files are there.

**Why `.gitignore` matters:** it keeps build output and local secret files out of GitHub. **Never commit passwords or keys.** That is exactly the problem Key Vault solves in Stage 4.

**2c. Watch CI run**

*What CI is:* a robot that builds your code on a clean machine every time you push. If the build breaks, you find out in minutes, not after release.

1. In the repo click the **Actions** tab. A run named **CI** started after your push (defined in `.github/workflows/ci.yml`, which is commented).
2. Open it. **Green tick** means restore, build and the Docker image check passed. **Red cross** means click the failed step and read the error.

**2d. Practice the team workflow (5 min)**

    git checkout -b change-greeting     # create and switch to a new branch
    # edit appsettings.json: change the HelloMessage text
    git add .
    git commit -m "Change greeting"
    git push -u origin change-greeting  # upload the branch

1. On GitHub click **Compare & pull request**, then **Create pull request**.
   *Why:* a PR is where teammates review and discuss before code reaches main.
2. Wait for the CI check on the PR to turn green.
   *Why:* checks must pass before merging.
3. Click **Merge pull request**. Back in the terminal run `git checkout main` then `git pull`.

**Done check:** a merged PR and a green CI run.

**Say it in the interview:** "We use short-lived branches, small pull requests, automatic CI checks, and review before merge. I keep secrets out of the repo."

---

## Stage 3: Azure basics, build the image, start AKS (10 min)

**Purpose:** create the Azure foundation and put your app in a container image that AKS can run.

**Concepts**
- A **subscription** is your billing account.
- A **resource group** is a folder for related resources. One delete removes everything, which makes cleanup safe and easy.
- A **container image** is your app packed with its runtime. **ACR** is the private store for images.
- Because AKS takes 5 to 10 minutes to create, we **start it now and let it run in the background**, and do Key Vault while waiting.

**Steps**

1. Sign in and confirm the subscription:

       az login               # opens a browser to sign in
       az account show        # shows the subscription you are using

   If you have several: `az account list -o table`, then `az account set --subscription "<name or id>"`.
   *Why check:* resources and charges go to the active subscription.
2. Register the services once (new subscriptions need this; each takes up to a minute):

       az provider register --namespace Microsoft.ContainerService    # AKS
       az provider register --namespace Microsoft.ContainerRegistry   # ACR
       az provider register --namespace Microsoft.KeyVault
       az provider register --namespace Microsoft.ApiManagement
       az provider register --namespace Microsoft.ManagedIdentity

   *Why:* Azure only lets you create a service type after it is registered for your subscription.
3. Set names once as variables:

       $RG   = "rg-hello"
       $LOC  = "canadacentral"
       $KV   = "kv-hello-<initials><digits>"
       $ACR  = "acrhello<initials><digits>"      # letters and numbers only
       $AKS  = "aks-hello"
       $APIM = "apim-hello-<initials><digits>"

   *Why variables:* you reuse these names in many commands, so one typo cannot break the lab. *Why add digits:* Key Vault, ACR and APIM names must be unique across all of Azure. If you close the terminal, paste this block again.
4. Create the resource group:

       az group create -n $RG -l $LOC

   *Why:* every other resource goes inside it.
5. Create the registry and build the image in Azure (run from the `HelloApi` folder):

       az acr create -n $ACR -g $RG --sku Basic     # the private image store
       az acr build -t helloapi:v1 -r $ACR .        # upload code, build with the Dockerfile, store the image

   *Why `acr build`:* Azure builds the image for you, so you do not need Docker on your laptop. The Dockerfile is commented, so read it while this runs.
6. Start AKS in the background:

       az aks create -g $RG -n $AKS --node-count 1 --node-vm-size Standard_B2s --attach-acr $ACR --enable-oidc-issuer --enable-workload-identity --generate-ssh-keys --no-wait

   What the options mean:
   - `--node-count 1`: one small worker machine, to keep cost down.
   - `--attach-acr`: lets the cluster pull images from your registry.
   - `--enable-oidc-issuer --enable-workload-identity`: needed in Stage 5b so pods can log in to Azure without passwords.
   - `--no-wait`: return now; Azure keeps creating it.

   If you get a quota or VM size error, list sizes with `az vm list-skus -l $LOC --size Standard_B -o table` and use another small size.
7. Look in the portal (portal.azure.com, Resource groups, rg-hello). Seeing the resources connects the commands to the real thing.

**Done check:** `az acr repository list -n $ACR -o table` shows `helloapi`.

**Say it in the interview:** "I group resources by resource group, build images in the registry, and script the infrastructure so it is repeatable."

---

## Stage 4: Key Vault (15 min, while AKS builds)

**Purpose:** keep secrets out of code, files and GitHub.

**Concepts**
- A **secret** is anything sensitive: a password, a connection string, an API key.
- **Key Vault** stores secrets securely, controls who can read them, and logs access.
- **RBAC** gives access by role. We use two roles: **Secrets Officer** (can write) for you, and **Secrets User** (read only) for the app. That is **least privilege**.
- **DefaultAzureCredential** is how the code logs in. On your laptop it uses your `az login`; in AKS it uses the pod's identity. The code is identical in both places.

**Steps**

1. Create the vault and give yourself write permission:

       az keyvault create -n $KV -g $RG --enable-rbac-authorization true
       $ME   = az ad signed-in-user show --query id -o tsv     # your user ID
       $KVID = az keyvault show -n $KV --query id -o tsv       # the vault's ID
       az role assignment create --assignee-object-id $ME --assignee-principal-type User --role "Key Vault Secrets Officer" --scope $KVID

   *Why the role:* even as owner of the subscription you need an explicit data role to write secrets. Wait about a minute for it to take effect.
2. Store a secret:

       az keyvault secret set --vault-name $KV --name HelloMessage --value "Hello from Key Vault"

3. Run the API against the vault:

       $env:KeyVault__Uri = "https://$KV.vault.azure.net/"   # turns on the Key Vault code in Program.cs
       dotnet run

   Open `/swagger` and call GET /api/hello. The message is now "Hello from Key Vault".
   *What happened:* `Program.cs` added the vault to configuration, so `_config["HelloMessage"]` found the secret. The controller did not change.
4. Prove the point: in the portal open Key Vault, Secrets, HelloMessage, **New version**, change the value, restart the app, call again. The answer changed and no code or file changed.
5. Stop the app and clear the setting: `Remove-Item Env:KeyVault__Uri`.

**Done check:** the API returned the Key Vault message.

**Say it in the interview:** "Secrets live in Key Vault, apps read them through a managed identity with read-only RBAC, nothing is stored in code or pipelines, and rotation does not need a code change."

---

## Stage 5: Deploy to AKS (25 min)

**Purpose:** run your container on Kubernetes and see why teams use it.

**Concepts (the YAML files are commented line by line)**
- **Pod:** runs your container.
- **Deployment:** "keep 2 copies running". If one dies, a new one starts. That is **self-healing**.
- **Service:** one steady address in front of pods whose IPs change. It spreads traffic across healthy pods.
- **Probes:** readiness ("send me traffic?") and liveness ("am I stuck?"), both calling /health.
- **Requests and limits:** what a pod reserves and the most it may use, so one pod cannot starve others.

**5a. Deploy the API**

1. Check the cluster is ready and connect `kubectl` (the Kubernetes command tool):

       az aks show -g $RG -n $AKS --query provisioningState -o tsv   # should print Succeeded
       az aks install-cli                                            # installs kubectl; reopen the terminal if asked
       az aks get-credentials -g $RG -n $AKS                         # points kubectl at your cluster

   If it says `Creating`, wait a few minutes.
2. In `k8s/deployment.yaml` replace `<ACR_NAME>` with your `$ACR` value (one line).
   *Why:* this tells Kubernetes where to pull your image from.
3. Deploy:

       kubectl apply -f k8s/        # create or update everything described in the YAML
       kubectl get pods             # wait for Running 1/1
       kubectl get svc helloapi     # wait for an EXTERNAL-IP

4. Browse to `http://<EXTERNAL-IP>/swagger` and `/health`. Your API now runs in Azure. The message is "Hello from appsettings.json".
5. **Play (10 minutes, this is where the learning is):**

       kubectl logs <pod-name>                        # the app's output, first stop when debugging
       kubectl describe pod <pod-name>                # events and reasons, second stop
       kubectl scale deployment helloapi --replicas=3 # scale out
       kubectl delete pod <pod-name>                  # kill one
       kubectl get pods                               # a new pod appears by itself

   *What you just saw:* self-healing and scaling. This is the answer to "why Kubernetes?".

**5b. Let pods read Key Vault (workload identity)**

*Purpose:* the pod must read the vault without any password. We give it an Azure identity and tell Azure to trust your pod's Kubernetes service account.

1. Create the identity and the trust link:

       az identity create -g $RG -n id-hello                                    # the Azure identity for the app
       $CLIENT_ID = az identity show -g $RG -n id-hello --query clientId -o tsv
       $ISSUER = az aks show -g $RG -n $AKS --query oidcIssuerProfile.issuerUrl -o tsv
       az identity federated-credential create --name fc-hello --identity-name id-hello -g $RG --issuer $ISSUER --subject system:serviceaccount:default:helloapi-sa --audience api://AzureADTokenExchange

   *Plain meaning of the last command:* "Azure, trust tokens from this cluster, but only for the service account `helloapi-sa` in the `default` namespace, and let them act as `id-hello`." The `--subject` must match exactly.
2. Give the identity read-only access to secrets:

       az role assignment create --assignee-object-id (az identity show -g $RG -n id-hello --query principalId -o tsv) --assignee-principal-type ServicePrincipal --role "Key Vault Secrets User" --scope $KVID

   *Why Secrets User:* read only, which is all the app needs (least privilege).
3. Edit two files:
   - `k8s/service.yaml`: replace `<MANAGED_IDENTITY_CLIENT_ID>` with the value of `$CLIENT_ID` (`echo $CLIENT_ID`).
   - `k8s/deployment.yaml`: set the `KeyVault__Uri` value to `https://<your vault name>.vault.azure.net/`.
4. Apply and restart:

       kubectl apply -f k8s/
       kubectl rollout restart deployment helloapi   # new pods pick up the new settings
       kubectl get pods

5. Call `/api/hello`. You should see "Hello from Key Vault", coming from inside AKS with no password anywhere.

**If it still shows the old message or fails:** wait two minutes (roles take time to apply), then `kubectl logs <pod-name>`. Check the client ID, the vault URL, the subject `system:serviceaccount:default:helloapi-sa`, and the role.

**Done check:** AKS returns the Key Vault message.

**Say it in the interview:** "I deploy with a Deployment and Service, use readiness and liveness probes and resource limits, scale horizontally, and use workload identity so pods reach Key Vault without secrets. If a pod fails I check logs and describe, then events, probes and config."

---

## Stage 6: APIM, the front door (15 min)

**Purpose:** put a gateway between callers and your backend.

**Why it matters:** instead of building keys, limits and rules into every API, you apply them once at the gateway. It also lets you change or hide the backend without callers noticing.

**Concepts**
- **Subscription key:** a key callers must send, so you know who is calling.
- **Policy:** XML rules applied at four stages: inbound, backend, outbound, on-error. Ours (commented in `apim-policy.xml`) rate-limits and adds and removes headers.
- **429 Too Many Requests:** the standard "slow down" answer.

**Steps**

1. Create the service (the Consumption tier is the cheapest way to learn; use your own email):

       az apim create -n $APIM -g $RG -l $LOC --publisher-email "<your email>" --publisher-name "HelloLab" --sku-name Consumption

   Or in the portal: Create a resource, API Management, tier Consumption. Wait until it finishes.
2. In the portal open APIM, **APIs**, **Add API**, **OpenAPI**. Specification URL: `http://<EXTERNAL-IP>/swagger/v1/swagger.json`. Set the URL suffix to `hello-lab`, then **Create**.
   *Why OpenAPI:* APIM reads the description Swagger generates and builds all the operations for you. If the import fails, choose **HTTP**, add the operations by hand, and use `http://<EXTERNAL-IP>` as the backend URL.
3. Add the rules: open your API, **All operations**, **Inbound processing**, the code icon (`</>`), replace the contents with `apim-policy.xml`, then save.
   *If the portal rejects the rate-limit block on your tier,* delete that one block and keep the rest.
4. Test: open the **Test** tab, pick GET /api/hello and **Send**. Look for the response header `X-Served-Via: APIM`. Or from PowerShell, after copying your key from **Subscriptions**:

       Invoke-WebRequest -Uri "https://$APIM.azure-api.net/hello-lab/api/hello" -Headers @{ "Ocp-Apim-Subscription-Key" = "<your key>" }

5. Call it more than 10 times in a minute: the 11th call returns **429**. Call without the key: **401**.
   *What you proved:* the gateway protects the backend before any call reaches it.

**Learning-setup warning:** the backend has a public IP only so APIM can reach it. In production keep the backend private and put APIM in front using private networking.

**Done check:** a 401 without the key and a 429 after too many calls.

**Say it in the interview:** "APIM is the single entry point. It handles authentication, rate limiting, caching, versioning and transformation with policies, so each backend stays simple. The backend still validates and authorizes."

---

## Stage 7: Cleanup (5 min)

**Purpose:** stop charges.

    az group delete -n $RG --yes --no-wait   # deletes everything inside the resource group

*Why one command works:* everything is in `rg-hello`. Check the portal later to confirm it is gone. Your GitHub repo and local code stay.

---

## If something goes wrong

| Problem | Why it happens | Try |
|---|---|---|
| `az` or `kubectl` not found | PATH is read when a terminal opens | Close and reopen the terminal; run `az aks install-cli` |
| Name already taken | Key Vault, ACR and APIM names are global | Add more digits |
| Provider not registered | Service type not enabled for the subscription | Run the Stage 3 register commands and wait a minute |
| AKS quota or VM size error | Region or size unavailable | Pick another small size with `az vm list-skus`, or another region |
| `ImagePullBackOff` | Cluster cannot find the image | Check `<ACR_NAME>` in the YAML and that `az acr build` finished |
| `CrashLoopBackOff` | App starts then crashes | `kubectl logs <pod>`; often a wrong vault URL or missing permission |
| Key Vault 403 | Role missing or not active yet | Wait two minutes; check role name and scope |
| No EXTERNAL-IP | Azure is still creating the public IP | Wait; run `kubectl get svc helloapi` again |
| Git login fails | Credentials not saved | Sign in via the browser window; Git for Windows includes the credential manager |
| Push rejected for email privacy | Real email exposed | Use the noreply email from GitHub Settings, Emails |

## What you can now say from experience

| Stage | Topics you can speak about |
|---|---|
| 1 | REST, status codes, validation, controllers vs minimal APIs, dependency injection, Swagger |
| 2 | Branching, pull requests, CI, why secrets stay out of repos |
| 3 | Subscriptions, resource groups, ACR, scripted infrastructure |
| 4 | Key Vault, RBAC, least privilege, managed identity, rotation |
| 5 | Deployment, Service, probes, limits, scaling, self-healing, workload identity, debugging pods |
| 6 | APIM gateway, subscription keys, policies, rate limiting |

## Five-minute interview story (memorize this)

"I built a small API and took it end to end. I wrote it in ASP.NET Core with validation and correct status codes, pushed it to GitHub where CI builds every change, and deployed it as a container to AKS with probes, limits and scaling. Secrets are in Key Vault and the pods read them through workload identity with read-only access, so nothing sensitive is in code. APIM sits in front for keys and rate limiting. It showed me the whole delivery path and what I would expect my team to own."
