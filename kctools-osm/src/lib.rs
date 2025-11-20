use std::{process::Command, result::Result};

use kc_osm::*;

#[unsafe(no_mangle)]
pub extern "Rust" fn module_init(_: &mut ScriptModule) -> Result<(), &'static str> {
    let services = services();

    let mode = match services.engine.config_get_raw("kctools_run") {
        Some(mode) => mode,
        None => return Err("'kctools_run' config value not defined."),
    };

    if mode != "light" {
        return Err("Unknown 'kctools_run' config value.");
    }

    let fm = match services.version.get_current_fm() {
        Some(fm) => fm,
        None => {
            return Err(
                "Failed to determine current FM. Make sure you are running DromEd in FM mode!",
            );
        }
    };

    let path = match services.engine.config_get_raw("kctools_path") {
        Some(p) => p,
        None => "Tools\\KCTools\\KCTools.exe".to_owned(),
    };

    services
        .debug
        .print(&format!("Running KCTools with path: {path}"));

    let output = match Command::new(path)
        .arg("light")
        .arg(".")
        .arg("KCLight.cow")
        .arg("-c")
        .arg(fm)
        .output()
    {
        Ok(output) => output,
        Err(_) => return Err("Failed to run KCTools."),
    };

    let output_lines = String::from_utf8_lossy(&output.stdout);
    for line in output_lines.lines() {
        services.debug.print(&line[13..]);
    }

    if !output.status.success() {
        return Err("Error occured while running KCTools.");
    }

    Ok(())
}
