### Setting up podman

`sudo apt install podman -y && sudo apt install podman-compose -y`

### Building and running the application

When you're ready, start the application by running:

`podman-compose up --build --force-recreate`

The application will be available at http://localhost:5002.
