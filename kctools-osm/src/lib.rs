use std::process::Command;

use kc_osm::*;

#[unsafe(no_mangle)]
pub extern "Rust" fn module_init(_: &mut ScriptModule) {
    let services = services();
    let fm = services.version.get_current_fm().unwrap();

    let output = Command::new("Tools\\KCTools\\KCTools.exe")
        .arg("light")
        .arg(".")
        .arg("KCLight.cow")
        .arg("-c")
        .arg(fm)
        .output()
        .unwrap();

    let output_lines = String::from_utf8_lossy(&output.stdout);
    for line in output_lines.lines() {
        services.debug.print(&line[13..]);
    }
}
